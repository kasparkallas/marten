using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Baseline;
using Marten.Events.Projections;
using Marten.Exceptions;
using Marten.Schema;
using Marten.Storage;
using Marten.Util;

namespace Marten.Events
{
    public enum StreamIdentity
    {
        AsGuid,
        AsString
    }

    public class EventGraph: IFeatureSchema
    {
        private readonly Ref<ImHashMap<string, IAggregator>> _aggregateByName =
            Ref.Of(ImHashMap<string, IAggregator>.Empty);

        private readonly Ref<ImHashMap<Type, IAggregator>> _aggregates =
            Ref.Of(ImHashMap<Type, IAggregator>.Empty);

        private readonly ConcurrentCache<string, EventMapping> _byEventName = new ConcurrentCache<string, EventMapping>();
        private readonly ConcurrentCache<Type, EventMapping> _events = new ConcurrentCache<Type, EventMapping>();

        private IAggregatorLookup _aggregatorLookup;
        private string _databaseSchemaName;

        public EventGraph(StoreOptions options)
        {
            Options = options;
            _aggregatorLookup = new AggregatorLookup();
            _events.OnMissing = eventType =>
            {
                var mapping = typeof(EventMapping<>).CloseAndBuildAs<EventMapping>(this, eventType);
                Options.Storage.AddMapping(mapping);

                return mapping;
            };

            _byEventName.OnMissing = name => { return AllEvents().FirstOrDefault(x => x.EventTypeName == name); };

            InlineProjections = new ProjectionCollection(options);
            AsyncProjections = new ProjectionCollection(options);
        }

        public StreamIdentity StreamIdentity { get; set; } = StreamIdentity.AsGuid;

        public TenancyStyle TenancyStyle { get; set; } = TenancyStyle.Single;

        /// <summary>
        ///     Whether a "for update" (row exclusive lock) should be used when selecting out the event version to use from the streams table
        /// </summary>
        /// <remkarks>
        ///     Not using this can result in race conditions in a concurrent environment that lead to
        ///       event version mismatches between the event and stream version numbers
        /// </remkarks>
        public bool UseAppendEventForUpdateLock { get; set; } = false;

        internal StoreOptions Options { get; }

        internal DbObjectName Table => new DbObjectName(DatabaseSchemaName, "mt_events");

        public EventMapping EventMappingFor(Type eventType)
        {
            return _events[eventType];
        }

        public EventMapping EventMappingFor<T>() where T : class
        {
            return EventMappingFor(typeof(T));
        }

        public IEnumerable<EventMapping> AllEvents()
        {
            return _events;
        }

        public IEnumerable<IAggregator> AllAggregates()
        {
            return _aggregates.Value.Enumerate().Select(x => x.Value);
        }

        public EventMapping EventMappingFor(string eventType)
        {
            return _byEventName[eventType];
        }

        public void AddEventType(Type eventType)
        {
            _events.FillDefault(eventType);
        }

        public void AddEventTypes(IEnumerable<Type> types)
        {
            types.Each(AddEventType);
        }

        public bool IsActive(StoreOptions options) => _events.Any() || _aggregates.Value.Enumerate().Any();

        public string DatabaseSchemaName
        {
            get { return _databaseSchemaName ?? Options.DatabaseSchemaName; }
            set { _databaseSchemaName = value; }
        }

        public void AddAggregator<T>(IAggregator<T> aggregator) where T : class
        {
            Options.Storage.MappingFor(typeof(T));
            _aggregates.Swap(a => a.AddOrUpdate(typeof(T), aggregator));
        }

        public IAggregator<T> AggregateFor<T>() where T : class
        {
            if (!_aggregates.Value.TryFind(typeof(T), out var aggregator))
            {
                Options.Storage.MappingFor(typeof(T));
                aggregator = _aggregatorLookup.Lookup<T>();
                _aggregates.Swap(a => a.AddOrUpdate(typeof(T), aggregator));
            }
            return aggregator.As<IAggregator<T>>();
        }

        public Type AggregateTypeFor(string aggregateTypeName)
        {
            if (_aggregateByName.Value.TryFind(aggregateTypeName, out var aggregate))
            {
                return aggregate.AggregateType;
            }

            aggregate = AllAggregates().FirstOrDefault(x => x.Alias == aggregateTypeName);
            if (aggregate == null)
            {
                return null;
            }

            _aggregateByName.Swap(a => a.AddOrUpdate(aggregateTypeName, aggregate));

            return aggregate.AggregateType;
        }

        public ProjectionCollection InlineProjections { get; }
        public ProjectionCollection AsyncProjections { get; }
        internal DbObjectName ProgressionTable => new DbObjectName(DatabaseSchemaName, "mt_event_progression");

        public string AggregateAliasFor(Type aggregateType)
        {
            if (!_aggregates.Value.TryFind(aggregateType, out var aggregator))
            {
                aggregator = _aggregatorLookup.Lookup(aggregateType);
                _aggregates.Swap(a => a.AddOrUpdate(aggregateType, aggregator));
            }

            return aggregator.Alias;
        }

        public IProjection ProjectionFor(Type viewType)
        {
            return AsyncProjections.ForView(viewType) ?? InlineProjections.ForView(viewType);
        }

        public ViewProjection<TView, TId> ProjectView<TView, TId>() where TView : class
        {
            var projection = new ViewProjection<TView, TId>();
            InlineProjections.Add(projection);
            return projection;
        }

        /// <summary>
        /// Set default strategy to lookup IAggregator when no explicit IAggregator registration exists.
        /// </summary>
        /// <remarks>Unless called, <see cref="AggregatorLookup"/> is used</remarks>
        public void UseAggregatorLookup(IAggregatorLookup aggregatorLookup)
        {
            _aggregatorLookup = aggregatorLookup;
        }

        IEnumerable<Type> IFeatureSchema.DependentTypes()
        {
            yield break;
        }

        ISchemaObject[] IFeatureSchema.Objects
        {
            get
            {
                var eventsTable = new EventsTable(this);

                // SAMPLE: using-sequence
                var sequence = new Sequence(new DbObjectName(DatabaseSchemaName, "mt_events_sequence"))
                {
                    Owner = eventsTable.Identifier,
                    OwnerColumn = "seq_id"
                };
                // ENDSAMPLE

                return new ISchemaObject[]
                {
                    new StreamsTable(this),
                    eventsTable,
                    new EventProgressionTable(DatabaseSchemaName),
                    sequence,

                    new AppendEventFunction(this),
                    new SystemFunction(DatabaseSchemaName, "mt_mark_event_progression", "varchar, bigint"),
                };
            }
        }

        Type IFeatureSchema.StorageType => typeof(EventGraph);
        public string Identifier { get; } = "eventstore";

        public void WritePermissions(DdlRules rules, StringWriter writer)
        {
            // Nothing
        }

        internal string GetStreamIdDBType()
        {
            return StreamIdentity == StreamIdentity.AsGuid ? "uuid" : "varchar";
        }

        internal Type GetStreamIdType()
        {
            return StreamIdentity == StreamIdentity.AsGuid ? typeof(Guid) : typeof(string);
        }

        private readonly Ref<ImHashMap<Type, string>> _dotnetTypeNames = Ref.Of(ImHashMap<Type, string>.Empty);

        internal string DotnetTypeNameFor(Type type)
        {
            if (!_dotnetTypeNames.Value.TryFind(type, out var value))
            {
                value = $"{type.FullName}, {type.GetTypeInfo().Assembly.GetName().Name}";

                _dotnetTypeNames.Swap(d => d.AddOrUpdate(type, value));
            }

            return value;
        }

        private readonly Ref<ImHashMap<string, Type>> _nameToType = Ref.Of(ImHashMap<string, Type>.Empty);

        internal Type TypeForDotNetName(string assemblyQualifiedName)
        {
            if (!_nameToType.Value.TryFind(assemblyQualifiedName, out var value))
            {
                value = Type.GetType(assemblyQualifiedName);
                if (value == null)
                {
                    throw new UnknownEventTypeException($"Unable to load event type '{assemblyQualifiedName}'.");
                }
                _nameToType.Swap(n => n.AddOrUpdate(assemblyQualifiedName, value));
            }

            return value;
        }
    }
}
