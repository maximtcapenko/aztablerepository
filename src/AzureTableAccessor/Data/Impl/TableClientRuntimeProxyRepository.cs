namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Builders;
    using Data;
    using Infrastructure;
    using Mappers;

    internal class TableClientRuntimeProxyRepository<TEntity> : BaseRuntimeRepository, IRepository<TEntity>
        where TEntity : class
    {
        private readonly Type _runtimeType;
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly TableClient _client;

        public TableClientRuntimeProxyRepository(TableServiceClient tableService, Type type,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers, ITableNameProvider tableNameProvider)
            : base(type)
        {
            _mappers = mappers;
            _runtimeType = type;
            _client = tableService.GetTableClient(tableNameProvider.GetTableName());
        }

        public Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var mapper = new ToRuntimeTypeMapper<TEntity>(entity, _mappers);
            var method = CreateMethodCreate();

            var factory = InstanceFactoryProvider.InstanceFactoryCache.GetOrAdd(_runtimeType,
                (t) => Expression.Lambda<Func<object>>(Expression.New(t)).Compile());

            var instance = factory();

            return method(this, new object[] {  mapper, instance, _client, cancellationToken });
        }

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var mapper = new ToRuntimeTypeMapper<TEntity>(entity, _mappers);
            var keys = _mappers.GetKeysFromEntity(entity);
            var method = CreateMethodUpdate();

            return method(this, new object[] { mapper, keys.partitionKey, keys.rowKey, _client, cancellationToken });
        }

        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var keys = _mappers.GetKeysFromEntity(entity);
            var method = CreateMethodDelete();

            return method(this, new object[] { keys.partitionKey, keys.rowKey, _client, cancellationToken });
        }

        public async Task<TEntity> LoadAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var keys = _mappers.GetKeysFromEntity(entity);
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var method = CreateMethodLoad();

            await method(this, new object[] { mapper, keys.partitionKey, keys.rowKey, _client, cancellationToken })
                .ConfigureAwait(false);

            entity = results.FirstOrDefault();
            return entity;
        }

        public async Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var query = new RuntimeQueryMapper<TEntity>(predicate, _mappers.OfType<ITranslateVisitorBuilderVisitor>());

            var method = CreateMethodGetSingle();

            await method(this, new object[] { null, query, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<TEntity>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var query = new RuntimeQueryMapper<TEntity>(predicate, _mappers.OfType<ITranslateVisitorBuilderVisitor>());

            var method = CreateMethodQuery();

            await method(this, new object[] { null, query, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            return results;
        }

        public async Task<IEnumerable<TEntity>> GetCollectionAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var method = CreateMethodGetAll();

            await method(this, new object[] { null, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            return results;
        }

        public async Task<Page<TEntity>> GetPageAsync(int pageSize = 100, string continuationToken = null,
             CancellationToken cancellationToken = default)
        {
            var results = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(results, _mappers);
            var tokens = new List<string>();
            var method = CreateMethodGetPage();

            await method(this, new object[] { null, mapper, tokens, continuationToken, pageSize, _client, cancellationToken })
                .ConfigureAwait(false);

            return new Page<TEntity>(results, tokens.FirstOrDefault(), pageSize);
        }
    }
}
