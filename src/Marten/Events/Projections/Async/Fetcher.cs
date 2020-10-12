using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Events.Projections.Async.ErrorHandling;
using Marten.Linq;
using Marten.Services;
using Marten.Storage;
using Marten.Util;
using Npgsql;
using NpgsqlTypes;

namespace Marten.Events.Projections.Async
{
    public class Fetcher: IDisposable, IFetcher
    {
        private readonly IDaemonLogger _logger;
        private readonly IDaemonErrorHandler _errorHandler;
        private readonly DaemonSettings _settings;
        private readonly AsyncOptions _options;
        private readonly IEventSelector _selector;
        private Task _fetchingTask;
        private long _lastEncountered;
        private CancellationToken _token = default(CancellationToken);
        private IProjectionTrack _track;
        private readonly string _sql;
        private readonly ITenant _tenant;
        private StreamIdentity _streamIdentity;

        public Fetcher(IDocumentStore store, DaemonSettings settings, AsyncOptions options, IDaemonLogger logger, IDaemonErrorHandler errorHandler, IEnumerable<Type> eventTypes)
        {
            _settings = settings;
            _options = options;
            _logger = logger;
            _errorHandler = errorHandler;
            State = FetcherState.Waiting;

            // TODO -- this will have to change
            _tenant = store.Tenancy.Default;

            _streamIdentity = store.Events.StreamIdentity;

            _selector = store.Events.StreamIdentity == StreamIdentity.AsGuid
                ? (IEventSelector)new EventSelector(store.Events, store.Advanced.Serializer)
                : new StringIdentifiedEventSelector(store.Events, store.Advanced.Serializer);

            EventTypeNames = eventTypes.Select(x => store.Events.EventMappingFor(x).Alias).ToArray();

            var fields = _selector.SelectFields().Join(", ");

            _sql =
    $@"
select seq_id from {_selector.Events.DatabaseSchemaName}.mt_events where seq_id > :last and seq_id <= :limit and extract(epoch from age(transaction_timestamp(), {_selector.Events.DatabaseSchemaName}.mt_events.timestamp)) >= :buffer order by seq_id;
select {fields} from {_selector.Events.DatabaseSchemaName}.mt_events where seq_id > :last and seq_id <= :limit and type = ANY(:types) and extract(epoch from age(transaction_timestamp(), {_selector.Events.DatabaseSchemaName}.mt_events.timestamp)) >= :buffer order by seq_id;
select min(seq_id) from {_selector.Events.DatabaseSchemaName}.mt_events where seq_id > :limit and type = ANY(:types) and extract(epoch from age(transaction_timestamp(), {_selector.Events.DatabaseSchemaName}.mt_events.timestamp)) >= :buffer;
select max(seq_id) from {_selector.Events.DatabaseSchemaName}.mt_events where seq_id >= :limit and extract(epoch from age(transaction_timestamp(), {_selector.Events.DatabaseSchemaName}.mt_events.timestamp)) >= :buffer
".Replace(" as d", "");
        }

        public Fetcher(DocumentStore store, DaemonSettings settings, IProjection projection, IDaemonLogger logger, IDaemonErrorHandler errorHandler)
            : this(store, settings, projection.AsyncOptions, logger, errorHandler, projection.Consumes)
        {
        }

        public string[] EventTypeNames { get; }

        public void Dispose()
        {
        }

        public void Start(IProjectionTrack track, DaemonLifecycle lifecycle, CancellationToken token = default(CancellationToken))
        {
            _track = track;

            if (_fetchingTask != null && !_fetchingTask.IsCompleted)
            {
                throw new InvalidOperationException("The Fetcher is already started!");
            }

            if (State == FetcherState.Active)
                return;

            _token = token;

            State = FetcherState.Active;

            if (track.LastEncountered > _lastEncountered)
            {
                _lastEncountered = track.LastEncountered;
            }

            _logger.FetchStarted(track);

            _fetchingTask =
                Task.Run(() => fetchEvents(track, lifecycle), token)
                    .ContinueWith(t =>
                    {
                        _logger.FetchingStopped(track);
                    }, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public Task Pause()
        {
            State = FetcherState.Paused;

            return _fetchingTask;
        }

        public Task Stop()
        {
            if (State != FetcherState.Active)
            {
                return Task.CompletedTask;
            }

            State = FetcherState.Waiting;

            return _fetchingTask;
        }

        public FetcherState State { get; private set; }

        public async Task<EventPage> FetchNextPage(long lastEncountered)
        {
            EventPage page = null;

            await _errorHandler.TryAction(async () =>
            {
                page = await fetchNextPage(lastEncountered).ConfigureAwait(false);
            }, _track).ConfigureAwait(false);

            return page;
        }

        public void Reset()
        {
            _lastEncountered = 0;
        }

        private async Task<EventPage> fetchNextPage(long lastEncountered)
        {
            using (var conn = _tenant.CreateConnection())
            {
                try
                {
                    await conn.OpenAsync(_token).ConfigureAwait(false);

                    var lastPossible = lastEncountered + _options.PageSize;

                    var cmd = conn.CreateCommand(_sql)
                        .With("last", lastEncountered)
                        .With("limit", lastPossible)
                        .With("buffer", _settings.LeadingEdgeBuffer.TotalSeconds)
                        .With("types", EventTypeNames, NpgsqlDbType.Array | NpgsqlDbType.Varchar);

                    var page = await buildEventPage(lastEncountered, cmd).ConfigureAwait(false);

                    if (page.Count == 0 || page.IsSequential())
                    {
                        return page;
                    }

                    var starting = page;

                    await Task.Delay(250, _token).ConfigureAwait(false);
                    page = await buildEventPage(lastEncountered, cmd).ConfigureAwait(false);
                    while (!page.CanContinueProcessing(starting.Sequences))
                    {
                        starting = page;
                        page = await buildEventPage(lastEncountered, cmd).ConfigureAwait(false);
                    }

                    return page;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private async Task<EventPage> buildEventPage(long from, NpgsqlCommand cmd)
        {
            IReadOnlyList<IEvent> events;
            IList<long> sequences = new List<long>();

            long nextKnown;
            long lastKnown;

            using (var reader = await cmd.ExecuteReaderAsync(_token).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(_token).ConfigureAwait(false))
                {
                    var seq = await reader.GetFieldValueAsync<long>(0, _token).ConfigureAwait(false);
                    sequences.Add(seq);
                }

                if (sequences.Any())
                {
                    await reader.NextResultAsync(_token).ConfigureAwait(false);

                    var list = new List<IEvent>();
                    while (await reader.ReadAsync(_token))
                    {
                        var @event = await _selector.ResolveAsync(reader, _token).ConfigureAwait(false);
                        list.Add(@event);
                    }

                    events = list;
                }
                else
                {
                    await reader.NextResultAsync(_token).ConfigureAwait(false);
                    events = new List<IEvent>();
                }

                nextKnown = await getLong(reader).ConfigureAwait(false);
                lastKnown = await getLong(reader).ConfigureAwait(false);
            }

            return new EventPage(from, sequences, events)
            {
                NextKnownSequence = nextKnown,
                LastKnownSequence = lastKnown,
                StreamIdentity = _streamIdentity
            };
        }

        private async Task<long> getLong(DbDataReader reader)
        {
            await reader.NextResultAsync(_token).ConfigureAwait(false);
            bool isAny = await reader.ReadAsync(_token).ConfigureAwait(false);

            if (!isAny)
                return 0;

            if (await reader.IsDBNullAsync(0, _token).ConfigureAwait(false))
            {
                return 0;
            }

            return await reader.GetFieldValueAsync<long>(0, _token).ConfigureAwait(false);
        }

        private async Task fetchEvents(IProjectionTrack track, DaemonLifecycle lifecycle)
        {
            while (!_token.IsCancellationRequested && State == FetcherState.Active)
            {
                var page = await FetchNextPage(_lastEncountered).ConfigureAwait(false);

                if (page.ShouldPause())
                {
                    if (lifecycle == DaemonLifecycle.Continuous)
                    {
                        State = FetcherState.Waiting;

                        _logger.PausingFetching(track, _lastEncountered);

#pragma warning disable 4014
                        Task.Delay(_settings.FetchingCooldown, _token).ContinueWith(t =>
                        {
                            Start(track, lifecycle, _token);
                        }, _token);
#pragma warning restore 4014
                    }
                    else
                    {
                        State = FetcherState.Paused;

                        _logger.FetchingIsAtEndOfEvents(track);
                        track.Finished(page.Ending());

                        _lastEncountered = page.LastEncountered();
                        track.QueuePage(page);

                        break;
                    }
                }

                _lastEncountered = page.LastEncountered();
                track.QueuePage(page);
            }
        }
    }
}
