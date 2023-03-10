namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Builders;
    using Data;
    using Infrastructure;
    using Infrastructure.Internal;
    using Mappers;
    using ILocalQueryProvider = AzureTableAccessor.Data.IQueryProvider;

    internal class TableClientRuntimeProxyProjectionRepository<TEntity, TProjection> : IRepository<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        private readonly Type _runtimeType;
        private readonly MethodInfo _querySingleMethod;
        private readonly MethodInfo _queryAllMethod;
        private readonly MethodInfo _queryMethod;
        private readonly MethodInfo _getPageMethod;
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly TableClient _client;
        private const string _cacheKeyPattern = "{0}-{1}";
        private readonly static ConcurrentDictionary<string, Func<object, object[], Task>> _methodsCache
            = new ConcurrentDictionary<string, Func<object, object[], Task>>();

        public TableClientRuntimeProxyProjectionRepository(TableServiceClient tableService, Type type,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers, ITableNameProvider tableNameProvider)
        {
            _mappers = mappers;
            _runtimeType = type;
            _client = tableService.GetTableClient(tableNameProvider.GetTableName());

            _querySingleMethod = GetType().FindNonPublicGenericMethod(nameof(SingleAsync));
            _queryAllMethod = GetType().FindNonPublicGenericMethod(nameof(GetCollectionAsync), 3);
            _queryMethod = GetType().FindNonPublicGenericMethod(nameof(GetCollectionAsync), 4);
            _getPageMethod = GetType().FindNonPublicGenericMethod(nameof(GetPageAsync));
        }

        public async Task<TProjection> SingleAsync(Expression<Func<TEntity, bool>> predicate,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var query = new RuntimeQueryMapper<TEntity>(predicate, _mappers.Where(e => e is ITranslateVisitorBuilderVisitor)
                .Select(e => e as ITranslateVisitorBuilderVisitor).ToList());
            var method = CreateExecutedMethod(_querySingleMethod);

            await method(this, new object[] { query, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            var projections = new List<TProjection>();

            return projections.FirstOrDefault();
        }

        public async Task<IEnumerable<TProjection>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var query = new RuntimeQueryMapper<TEntity>(predicate, _mappers.Where(e => e is ITranslateVisitorBuilderVisitor)
                .Select(e => e as ITranslateVisitorBuilderVisitor).ToList());
            var method = CreateExecutedMethod(_queryMethod);

            await method(this, new object[] { query, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            var projections = new List<TProjection>();

            return projections;
        }

        public async Task<IEnumerable<TProjection>> GetCollectionAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var method = CreateExecutedMethod(_queryAllMethod);

            await method(this, new object[] { mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            var projections = new List<TProjection>();

            return projections;
        }

        public async Task<Page<TProjection>> GetPageAsync(int pageSize = 100, string continuationToken = null,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var genericMethod = _getPageMethod.MakeGenericMethod(_runtimeType);
            var tokens = new List<string>();
            var method = CreateExecutedMethod(_getPageMethod);

            await method(this, new object[] { mapper, tokens, continuationToken, pageSize, _client, cancellationToken })
                .ConfigureAwait(false);

            var projections = new List<TProjection>();

            return new Page<TProjection>(projections, tokens.FirstOrDefault(), pageSize);
        }

        private Func<object, object[], Task> CreateExecutedMethod(MethodInfo methodInfo)
            => _methodsCache.GetOrAdd($"{methodInfo.Name}-{(_runtimeType.Name)}-{methodInfo.GetParameters().Count()}",
             key =>
            {
                var genericMethod = methodInfo.MakeGenericMethod(_runtimeType);
                return MethodFactory.CreateGenericMethod<Task>(genericMethod);
            });

        #region Runtime methods
        private async Task GetCollectionAsync<T>(IEnumerable<string> selector, IMapper mapper, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync<T>(select: selector, cancellationToken: cancellationToken).AsPages();

            await foreach (var page in pages.ConfigureAwait(false))
            {
                if (page.Values != null)
                {
                    foreach (var entity in page.Values)
                        mapper.Map(entity);
                }
            }
        }

        private async Task GetPageAsync<T>(IEnumerable<string> selector, IMapper mapper, List<string> tokens, string continuationToken, int pageSize,
             TableClient client, CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync<T>(select: selector, cancellationToken: cancellationToken).AsPages(continuationToken, pageSize);

            var pageEnumerator = pages.GetAsyncEnumerator();
            await pageEnumerator.MoveNextAsync().ConfigureAwait(false);

            var page = pageEnumerator.Current;
            if (page.Values != null)
            {
                foreach (var entity in page.Values)
                    mapper.Map(entity);
            }
            if (page.ContinuationToken != null)
                tokens.Add(page.ContinuationToken);
        }

        private async Task GetCollectionAsync<T>(IEnumerable<string> selector, ILocalQueryProvider query, IMapper mapper, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync(query.Query<T>(), select: selector, cancellationToken: cancellationToken).AsPages();

            await foreach (var page in pages.ConfigureAwait(false))
            {
                if (page.Values != null)
                {
                    foreach (var entity in page.Values)
                        mapper.Map(entity);
                }
            }
        }

        private async Task SingleAsync<T>(IEnumerable<string> selector, ILocalQueryProvider query, IMapper mapper, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync(query.Query<T>(), select: selector, cancellationToken: cancellationToken).AsPages(pageSizeHint: 1);
            var enumerator = pages.GetAsyncEnumerator();
            await enumerator.MoveNextAsync().ConfigureAwait(false);

            var page = enumerator.Current;
            if (page.Values != null)
            {
                foreach (var entity in page.Values)
                    mapper.Map(entity);
            }
        }
        #endregion
    }
}
