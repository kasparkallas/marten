using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Marten.Internal;
using Marten.Linq.Parsing;
using Marten.Linq.QueryHandlers;
using Marten.Util;
using Remotion.Linq.Clauses;

namespace Marten.Linq
{
    internal class LinqQueryProvider: IQueryProvider
    {
        private readonly IMartenSession _session;

        public LinqQueryProvider(IMartenSession session)
        {
            _session = session;
        }

        internal QueryStatistics Statistics { get; set; }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MartenLinqQueryable<TElement>(_session, this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotSupportedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var builder = new LinqHandlerBuilder(_session, expression);
            var handler = builder.BuildHandler<TResult>(Statistics);

            return ExecuteHandler(handler);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            var builder = new LinqHandlerBuilder(_session, expression);
            var handler = builder.BuildHandler<TResult>(Statistics);

            return ExecuteHandlerAsync(handler, token);
        }

        public TResult Execute<TResult>(Expression expression, ResultOperatorBase op)
        {
            var builder = new LinqHandlerBuilder(_session, expression, op);
            var handler = builder.BuildHandler<TResult>(Statistics);

            return ExecuteHandler(handler);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token, ResultOperatorBase op)
        {
            var builder = new LinqHandlerBuilder(_session, expression, op);
            var handler = builder.BuildHandler<TResult>(Statistics);

            return ExecuteHandlerAsync(handler, token);
        }

        public async Task<T> ExecuteHandlerAsync<T>(IQueryHandler<T> handler, CancellationToken token)
        {
            var cmd = _session.BuildCommand(handler);

            using var reader = await _session.Database.ExecuteReaderAsync(cmd, token).ConfigureAwait(false);
            return await handler.HandleAsync(reader, _session, token).ConfigureAwait(false);
        }

        public T ExecuteHandler<T>(IQueryHandler<T> handler)
        {
            var cmd = _session.BuildCommand(handler);

            using var reader = _session.Database.ExecuteReader(cmd);
            return handler.Handle(reader, _session);
        }


    }
}
