using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Linq;
using Marten.Linq.Includes;
using Npgsql;

namespace Marten
{
    public static class QueryableExtensions
    {
        public static QueryPlan Explain<T>(this IQueryable<T> queryable, Action<IConfigureExplainExpressions> configureExplain = null)
        {
            return queryable.As<IMartenQueryable<T>>().Explain(configureExplain: configureExplain);
        }

        #region ToList

        public static Task<IReadOnlyList<T>> ToListAsync<T>(this IQueryable<T> queryable,
            CancellationToken token = default(CancellationToken))
        {
            return queryable.As<IMartenQueryable>().ToListAsync<T>(token);
        }

        #endregion ToList

        #region Any

        public static Task<bool> AnyAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().AnyAsync(token);
        }

        public static Task<bool> AnyAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).AnyAsync(token);
        }

        #endregion Any

        #region Aggregate Functions

        public static Task<TResult> SumAsync<TSource, TResult>(
            this IQueryable<TSource> source, Expression<Func<TSource, TResult>> expression,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(expression).As<IMartenQueryable>().SumAsync<TResult>(token);
        }

        public static Task<TResult> MaxAsync<TSource, TResult>(
            this IQueryable<TSource> source, Expression<Func<TSource, TResult>> expression,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(expression).As<IMartenQueryable>().MaxAsync<TResult>(token);
        }

        public static Task<TResult> MinAsync<TSource, TResult>(
            this IQueryable<TSource> source, Expression<Func<TSource, TResult>> expression,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(expression).As<IMartenQueryable>().MinAsync<TResult>(token);
        }

        public static Task<double> AverageAsync<TSource, TMember>(
            this IQueryable<TSource> source, Expression<Func<TSource, TMember>> expression,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(expression).As<IMartenQueryable>().AverageAsync(token);
        }

        #endregion Aggregate Functions

        #region Count/LongCount/Sum

        public static Task<int> CountAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().CountAsync(token);
        }

        public static Task<int> CountAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).CountAsync(token);
        }

        public static Task<long> LongCountAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().CountLongAsync(token);
        }

        public static Task<long> LongCountAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).LongCountAsync(token);
        }

        #endregion Count/LongCount/Sum

        #region First/FirstOrDefault

        public static Task<TSource> FirstAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().FirstAsync<TSource>(token);
        }

        public static Task<TSource> FirstAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).FirstAsync(token);
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().FirstOrDefaultAsync<TSource>(token);
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).FirstOrDefaultAsync(token);
        }

        #endregion First/FirstOrDefault

        #region Single/SingleOrDefault

        public static Task<TSource> SingleAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().SingleAsync<TSource>(token);
        }

        public static Task<TSource> SingleAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).SingleAsync(token);
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.As<IMartenQueryable>().SingleOrDefaultAsync<TSource>(token);
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return source.Where(predicate).SingleOrDefaultAsync(token);
        }

        #endregion Single/SingleOrDefault

        #region Shared

        private static IMartenQueryable<T> CastToMartenQueryable<T>(IQueryable<T> queryable)
        {
            var martenQueryable = queryable as IMartenQueryable<T>;
            if (martenQueryable == null)
            {
                throw new InvalidOperationException($"{typeof(T)} is not IMartenQueryable<>");
            }

            return martenQueryable;
        }

        #endregion Shared

        public static NpgsqlCommand ToCommand<T>(this IQueryable<T> queryable, FetchType fetchType = FetchType.FetchMany)
        {
            if (queryable is MartenLinqQueryable<T> q1)
            {
                return q1.ToPreviewCommand(fetchType);
            }

            throw new InvalidOperationException($"{nameof(ToCommand)} is only valid on Marten IQueryable objects");
        }

        public static IMartenQueryable<T> Include<T, TInclude>(this IQueryable<T> queryable,
            Expression<Func<T, object>> idSource,
            Action<TInclude> callback)
        {
            var include = queryable.As<MartenLinqQueryable<T>>().BuildInclude(idSource,callback);
            return queryable.Select(x => x.IncludePlan(include)).As<IMartenQueryable<T>>();
        }

        public static IMartenQueryable<T> Include<T, TInclude>(this IQueryable<T> queryable,
            Expression<Func<T, object>> idSource,
            IList<TInclude> list)
        {
            var include = queryable.As<MartenLinqQueryable<T>>().BuildInclude(idSource,(Action<TInclude>) list.Add);
            return queryable.Select(x => x.IncludePlan(include)).As<IMartenQueryable<T>>();
        }

        public static IMartenQueryable<T> Include<T, TKey, TInclude>(this IQueryable<T> queryable,
            Expression<Func<T, object>> idSource,
            IDictionary<TKey, TInclude> dictionary)
        {
            var include = queryable.As<MartenLinqQueryable<T>>().BuildInclude(idSource, dictionary);
            return queryable.Select(x => x.IncludePlan(include)).As<IMartenQueryable<T>>();
        }


        /// <summary>
        /// This is only here to sneak an IInclude into a Linq expression
        /// </summary>
        /// <param name="target"></param>
        /// <param name="include"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static T IncludePlan<T>(this T target, IIncludePlan include)
        {
            return target;
        }
    }
}
