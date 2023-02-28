namespace AzureTableAccessor.Data.Impl
{
    using Azure.Data.Tables;
    using Builders;
    using System;
    using System.Collections.Generic;
    using Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Mappers;
    using System.Collections.Concurrent;

    internal class TableClientRuntimeProxyRepository<TEntity> : IRepository<TEntity>
        where TEntity : class
    {
        private readonly Type _runtimeType;
        private readonly TableServiceClient _tableService;
        private readonly MethodInfo _createMethod;
        private readonly MethodInfo _updateMethod;
        private readonly MethodInfo _deleteMethod;
        private readonly MethodInfo _loadMethod;
        private readonly MethodInfo _querySingleMethod;
        private readonly MethodInfo _queryAllMethod;
        private readonly MethodInfo _queryMethod;
        private readonly MethodInfo _queryPageMethod;
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly TableClient _client;
        private readonly static ConcurrentDictionary<Type, Func<object>> _instanceFactoryCache
         = new ConcurrentDictionary<Type, Func<object>>();

        public TableClientRuntimeProxyRepository(TableServiceClient tableService, Type type, IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
        {
            _mappers = mappers;
            _runtimeType = type;
            _tableService = tableService;
            _client = _tableService.GetTableClient(typeof(TEntity).Name.ToLower());

            _createMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(CreateAsync));

            _updateMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(UpdateAsync));

            _deleteMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(DeleteAsync));

            _loadMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(LoadAsync));

            _querySingleMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(SingleAsync));

            _queryAllMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetParameters().Length == 2 && m.IsGenericMethod && m.Name == nameof(GetCollectionAsync));

            _queryMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetParameters().Length == 3 && m.IsGenericMethod && m.Name == nameof(GetCollectionAsync));

            _queryPageMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetParameters().Length == 4 && m.IsGenericMethod && m.Name == nameof(GetPageAsync));
        }

        public Task CreateAsync(TEntity entity)
        {
            var mapper = new ToRuntimeTypeMapper(entity, _mappers);
            var genericMethod = _createMethod.MakeGenericMethod(_runtimeType);

            var factory = _instanceFactoryCache.GetOrAdd(_runtimeType,
                (t) => Expression.Lambda<Func<object>>(Expression.New(t)).Compile());

            var instance = factory();

            return (Task)genericMethod.Invoke(this, new object[] { mapper, instance, _client });
        }

        public Task UpdateAsync(TEntity entity)
        {
            var mapper = new ToRuntimeTypeMapper(entity, _mappers);
            var genericMethod = _updateMethod.MakeGenericMethod(_runtimeType);
            var keys = _mappers.GetKeysFromEntity(entity);

            return (Task)genericMethod.Invoke(this, new object[] { mapper, keys.partitionKey, keys.rowKey, _client });
        }

        public Task DeleteAsync(TEntity entity)
        {
            var genericMethod = _deleteMethod.MakeGenericMethod(_runtimeType);
            var keys = _mappers.GetKeysFromEntity(entity);

            return (Task)genericMethod.Invoke(this, new object[] { keys.partitionKey, keys.rowKey, _client });
        }

        public async Task<TEntity> LoadAsync(TEntity entity)
        {
            var genericMethod = _loadMethod.MakeGenericMethod(_runtimeType);
            var keys = _mappers.GetKeysFromEntity(entity);
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper(results, _mappers);

            await (Task)genericMethod.Invoke(this, new object[] { mapper, keys.partitionKey, keys.rowKey, _client });

            return results.FirstOrDefault();
        }

        public async Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper(results, _mappers);

            var genericMethod = _querySingleMethod.MakeGenericMethod(_runtimeType);

            //map expression
            var query = new RuntimeQueryMapper(predicate, _mappers.Where(e => e is ITranslateVisitorBuilderVisitor)
                .Select(e => e as ITranslateVisitorBuilderVisitor).ToList());

            await (Task)genericMethod.Invoke(this, new object[] { query, mapper, _client });

            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<TEntity>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper(results, _mappers);

            var genericMethod = _queryMethod.MakeGenericMethod(_runtimeType);

            //map expression
            var query = new RuntimeQueryMapper(predicate, _mappers.Where(e => e is ITranslateVisitorBuilderVisitor)
                .Select(e => e as ITranslateVisitorBuilderVisitor).ToList());

            await (Task)genericMethod.Invoke(this, new object[] { query, mapper, _client });

            return results;
        }

        public async Task<IEnumerable<TEntity>> GetCollectionAsync()
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper(results, _mappers);
            var genericMethod = _queryAllMethod.MakeGenericMethod(_runtimeType);

            await (Task)genericMethod.Invoke(this, new object[] { mapper, _client });

            return results;
        }

        public async Task<Page<TEntity>> GetPageAsync(int pageSize = 100, string continuationToken = null)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper(results, _mappers);
            var genericMethod = _queryPageMethod.MakeGenericMethod(_runtimeType);

            var token = await (Task<string>)genericMethod.Invoke(this, new object[] { mapper, continuationToken, pageSize, _client });

            return new Page<TEntity>(results, token, pageSize);
        }

        private async Task CreateAsync<T>(IMapper mapper, T entity, TableClient client) where T : class, ITableEntity, new()
        {
            mapper.Map(entity);

            await client.CreateIfNotExistsAsync();

            await client.AddEntityAsync(entity);
        }

        private async Task UpdateAsync<T>(IMapper mapper, string partitionKey, string rowKey, TableClient client) where T : class, ITableEntity, new()
        {
            var response = await client.GetEntityAsync<T>(partitionKey, rowKey);
            var entity = response.Value;

            mapper.Map(entity);

            await client.UpdateEntityAsync(entity, entity.ETag);
        }

        private async Task DeleteAsync<T>(string partitionKey, string rowKey, TableClient client) where T : class, ITableEntity, new()
        {
            var response = await client.GetEntityAsync<T>(partitionKey, rowKey);
            var entity = response.Value;

            if (entity != null)
                await client.DeleteEntityAsync(partitionKey, rowKey, entity.ETag);
        }

        private async Task LoadAsync<T>(IMapper mapper, string partitionKey, string rowKey, TableClient client) where T : class, ITableEntity, new()
        {
            ArgumentNullException.ThrowIfNull(partitionKey, nameof(partitionKey));
            ArgumentNullException.ThrowIfNull(rowKey, nameof(rowKey));

            var result = await client.GetEntityAsync<T>(partitionKey, rowKey);
            if (result.Value != null)
                mapper.Map(result.Value);
        }

        private async Task GetCollectionAsync<T>(IMapper mapper, TableClient client) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync<T>().AsPages();

            await foreach (var page in pages)
            {
                if (page.Values != null)
                {
                    foreach (var entity in page.Values)
                        mapper.Map(entity);
                }
            }
        }

        private async Task<string> GetPageAsync<T>(IMapper mapper, string continuationToken, int pageSize, TableClient client) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync<T>().AsPages(continuationToken, pageSize);

            var pageEnumerator = pages.GetAsyncEnumerator();
            await pageEnumerator.MoveNextAsync();

            var page = pageEnumerator.Current;
            if (page.Values != null)
            {
                foreach (var entity in page.Values)
                    mapper.Map(entity);
            }

            continuationToken = page.ContinuationToken;

            return continuationToken;
        }

        private async Task GetCollectionAsync<T>(IQueryProvider query, IMapper mapper, TableClient client) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync(query.Query<T>()).AsPages();

            await foreach (var page in pages)
            {
                if (page.Values != null)
                {
                    foreach (var entity in page.Values)
                        mapper.Map(entity);
                }
            }
        }

        private async Task SingleAsync<T>(IQueryProvider query, IMapper mapper, TableClient client) where T : class, ITableEntity, new()
        {
            var pages = client.QueryAsync(query.Query<T>()).AsPages(pageSizeHint: 1);
            var enumerator = pages.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();

            var page = enumerator.Current;
            if (page.Values != null)
            {
                foreach (var entity in page.Values)
                    mapper.Map(entity);
            }
        }

        internal interface IQueryProvider
        {
            Expression<Func<T, bool>> Query<T>() where T : class, ITableEntity, new();
        }

        internal interface IMapper
        {
            void Map<T>(T obj) where T : class, ITableEntity, new();
        }

        internal class RuntimeQueryMapper : IQueryProvider
        {
            private readonly Expression<Func<TEntity, bool>> _predicate;
            private readonly IEnumerable<ITranslateVisitorBuilderVisitor> _builderVisitors;

            public RuntimeQueryMapper(Expression<Func<TEntity, bool>> predicate,
                IEnumerable<ITranslateVisitorBuilderVisitor> builderVisitors)
            {
                _builderVisitors = builderVisitors;
                _predicate = predicate;
            }

            public Expression<Func<T, bool>> Query<T>() where T : class, ITableEntity, new()
            {
                var builder = new TranslateVisitorBuilder<T>();
                foreach (var visitor in _builderVisitors) visitor.Visit(builder);

                var translator = builder.Build();
                translator.Visit(_predicate);
                var translation = translator.GetTranslatedExpression();

                //build lambda
                var predicate = Expression.Lambda<Func<T, bool>>(translation, builder.ParameterExpression);
                return predicate;
            }

        }

        internal class FromRuntimeTypeMapper : IMapper
        {
            private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;

            private readonly List<TEntity> _entities;

            public FromRuntimeTypeMapper(List<TEntity> entities,
                IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
            {
                _entities = entities;
                _mappers = mappers;
            }

            public void Map<T>(T obj) where T : class, ITableEntity, new()
            {
                var factory = _instanceFactoryCache.GetOrAdd(typeof(TEntity),
                (type) => Expression.Lambda<Func<object>>(Expression.New(type)).Compile());

                var entity = factory() as TEntity;

                _entities.Add(entity);

                foreach (var mapper in _mappers)
                {
                    mapper.Map(obj, entity);
                }
            }
        }

        internal class ToRuntimeTypeMapper : IMapper
        {
            private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
            private readonly TEntity _entity;

            public ToRuntimeTypeMapper(TEntity entity, IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
            {
                _entity = entity;
                _mappers = mappers;
            }

            public void Map<T>(T obj) where T : class, ITableEntity, new()
            {
                foreach (var mapper in _mappers) mapper.Map(_entity, obj);
            }
        }
    }
}
