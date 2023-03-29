namespace AzureTableAccessor.Data.Impl.Repositories
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Infrastructure;
    using Infrastructure.Internal;
    using ILocalQueryProvider = AzureTableAccessor.Data.IQueryProvider;

    internal class BaseRuntimeRepository
    {
        private readonly Type _runtimeType;
        private readonly MethodInfo _createMethod;
        private readonly MethodInfo _updateMethod;
        private readonly MethodInfo _deleteMethod;
        private readonly MethodInfo _loadMethod;
        private readonly MethodInfo _querySingleMethod;
        private readonly MethodInfo _queryAllMethod;
        private readonly MethodInfo _queryMethod;
        private readonly MethodInfo _getPageMethod;
        private readonly IEntityCache _entityCache;
        private readonly static ConcurrentDictionary<string, Func<object, object[], Task>> _methodsCache
            = new ConcurrentDictionary<string, Func<object, object[], Task>>();

        public BaseRuntimeRepository(Type runtimeType, IEntityCache entityCache)
        {
            _entityCache = entityCache;
            _runtimeType = runtimeType;
            _createMethod = GetType().FindNonPublicGenericMethod(nameof(CreateAsync));
            _updateMethod = GetType().FindNonPublicGenericMethod(nameof(UpdateAsync));
            _deleteMethod = GetType().FindNonPublicGenericMethod(nameof(DeleteAsync));
            _loadMethod = GetType().FindNonPublicGenericMethod(nameof(LoadAsync));

            _querySingleMethod = GetType().FindNonPublicGenericMethod(nameof(SingleAsync));
            _queryAllMethod = GetType().FindNonPublicGenericMethod(nameof(GetCollectionAsync), 4);
            _queryMethod = GetType().FindNonPublicGenericMethod(nameof(GetCollectionAsync), 5);
            _getPageMethod = GetType().FindNonPublicGenericMethod(nameof(GetPageAsync));
        }

        protected Func<object, object[], Task> CreateMethodCreate() => CreateRuntimeMethod(_createMethod);
        protected Func<object, object[], Task> CreateMethodUpdate() => CreateRuntimeMethod(_updateMethod);
        protected Func<object, object[], Task> CreateMethodDelete() => CreateRuntimeMethod(_deleteMethod);
        protected Func<object, object[], Task> CreateMethodLoad() => CreateRuntimeMethod(_loadMethod);
        protected Func<object, object[], Task> CreateMethodGetAll() => CreateRuntimeMethod(_queryAllMethod);
        protected Func<object, object[], Task> CreateMethodQuery() => CreateRuntimeMethod(_queryMethod);
        protected Func<object, object[], Task> CreateMethodGetPage() => CreateRuntimeMethod(_getPageMethod);
        protected Func<object, object[], Task> CreateMethodGetSingle() => CreateRuntimeMethod(_querySingleMethod);

        protected Func<object, object[], Task> CreateRuntimeMethod(MethodInfo methodInfo)
        => _methodsCache.GetOrAdd($"{methodInfo.Name}-{(_runtimeType.Name)}-{methodInfo.GetParameters().Count()}",
        key =>
        {
            var genericMethod = methodInfo.MakeGenericMethod(_runtimeType);
            return MethodFactory.CreateGenericMethod<Task>(genericMethod);
        });

        private async Task CreateAsync<T>(IAutoKeyGenerator autoKeyGenerator,
            ITransactionBuilder transactionBuilder, 
            IMapper inMapper, 
            IMapper outMapper, T entity, TableClient client,
            CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            inMapper.Map(entity);

            if (transactionBuilder != null)
            {
                transactionBuilder.CreateEntity(entity, response =>
                {
                    if (response.Headers.ETag.HasValue)
                        entity.ETag = response.Headers.ETag.Value;

                    entity.Timestamp = response.Headers.Date;

                    outMapper.Map(entity);
                    _entityCache.Add(entity);
                });

                return;
            }
            
            if(autoKeyGenerator != null)
            {
                entity.PartitionKey = autoKeyGenerator.Generate();
            }

            await client.CreateIfNotExistsAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = await client.AddEntityAsync(entity, cancellationToken)
                .ConfigureAwait(false);

            if (result.Headers.ETag.HasValue)
                entity.ETag = result.Headers.ETag.Value;

            entity.Timestamp = result.Headers.Date;
            
            outMapper.Map(entity);
            _entityCache.Add(entity);
        }

        private async Task UpdateAsync<T>(ITransactionBuilder transactionBuilder, IMapper mapper, string partitionKey, string rowKey, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            T entity = null;
            try
            {
                entity = _entityCache.Get<T>(partitionKey, rowKey);

                if (entity == null)
                {
                    var response = await client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    entity = response.Value;
                }
                mapper.Map(entity);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                throw new EntityNotFoundException(partitionKey, rowKey);
            }

            if (transactionBuilder != null)
            {
                transactionBuilder.UpdateEntity(entity, response =>
                {
                    if (response.Headers.ETag.HasValue)
                        entity.ETag = response.Headers.ETag.Value;

                    entity.Timestamp = response.Headers.Date;

                    _entityCache.Add(entity);
                });

                return;
            }

            var updated = await client.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (updated.Headers.ETag.HasValue)
                entity.ETag = updated.Headers.ETag.Value;

            entity.Timestamp = updated.Headers.Date;
            _entityCache.Add(entity);
        }

        private async Task DeleteAsync<T>(ITransactionBuilder transactionBuilder, string partitionKey, string rowKey, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            T entity = null;
            try
            {
                entity = _entityCache.Get<T>(partitionKey, rowKey);
                if (entity == null)
                {
                    var response = await client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    entity = response.Value;
                }
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                throw new EntityNotFoundException(partitionKey, rowKey);
            }

            if(transactionBuilder != null)
            {
                transactionBuilder.DeleteEntity(entity, response => 
                {
                    _entityCache.Remove(partitionKey, rowKey);
                });
                
                return;
            }

            await client.DeleteEntityAsync(partitionKey, rowKey, entity.ETag, cancellationToken)
                .ConfigureAwait(false);

            _entityCache.Remove(partitionKey, rowKey);
        }

        private async Task LoadAsync<T>(IMapper mapper, string partitionKey, string rowKey, TableClient client,
             CancellationToken cancellationToken) where T : class, ITableEntity, new()
        {
            if (string.IsNullOrEmpty(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
            if (string.IsNullOrEmpty(rowKey)) throw new ArgumentNullException(nameof(rowKey));

            try
            {
                var result = await client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _entityCache.Add(result.Value);
                mapper.Map(result.Value);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {

            }
        }

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
    }
}