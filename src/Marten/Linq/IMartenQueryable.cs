using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Marten.Linq
{
    public interface IMartenQueryable
    {
        QueryStatistics Statistics { get; }

        Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken token);

        Task<bool> AnyAsync(CancellationToken token);

        Task<int> CountAsync(CancellationToken token);

        Task<long> CountLongAsync(CancellationToken token);

        Task<TResult> FirstAsync<TResult>(CancellationToken token);

        Task<TResult> FirstOrDefaultAsync<TResult>(CancellationToken token);

        Task<TResult> SingleAsync<TResult>(CancellationToken token);

        Task<TResult> SingleOrDefaultAsync<TResult>(CancellationToken token);

        Task<TResult> SumAsync<TResult>(CancellationToken token);

        Task<TResult> MinAsync<TResult>(CancellationToken token);

        Task<TResult> MaxAsync<TResult>(CancellationToken token);

        Task<double> AverageAsync(CancellationToken token);

        /// <param name="configureExplain">Configure EXPLAIN options as documented in <see href="https://www.postgresql.org/docs/9.6/static/sql-explain.html">EXPLAIN documentation</see></param>
        QueryPlan Explain(FetchType fetchType = FetchType.FetchMany, Action<IConfigureExplainExpressions> configureExplain = null);

        /// <summary>
        ///     Applies a pre-loaded Javascript transformation to the documents
        ///     returned by this query
        /// </summary>
        /// <typeparam name="TDoc"></typeparam>
        /// <param name="transformName"></param>
        /// <returns></returns>
        IQueryable<TDoc> TransformTo<TDoc>(string transformName);

        /// <summary>
        /// Retrieve the document data as a JSON array string
        /// </summary>
        /// <returns></returns>
        string ToJsonArray();

        /// <summary>
        /// Retrieve the document data as a JSON array string
        /// </summary>
        /// <returns></returns>
        Task<string> ToJsonArrayAsync(CancellationToken token);
    }

    public interface IMartenQueryable<T>: IQueryable<T>, IMartenQueryable
    {
        IMartenQueryable<T> Include<TInclude>(Expression<Func<T, object>> idSource, Action<TInclude> callback);

        IMartenQueryable<T> Include<TInclude>(Expression<Func<T, object>> idSource, IList<TInclude> list);

        IMartenQueryable<T> Include<TInclude, TKey>(Expression<Func<T, object>> idSource,
            IDictionary<TKey, TInclude> dictionary);

        IMartenQueryable<T> Stats(out QueryStatistics stats);

    }
}
