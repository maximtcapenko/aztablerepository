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

    internal class TableClientRuntimeProxyProjectionRepository<TEntity, TProjection> :
        BaseRuntimeRepository, IRepository<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        private readonly Type _runtimeType;
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity, TProjection>> _projectionMappers;
        private readonly TableClient _client;

        public TableClientRuntimeProxyProjectionRepository(TableServiceClient tableService, Type type,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers,
            IEnumerable<IPropertyRuntimeMapper<TEntity, TProjection>> pmappers, ITableNameProvider tableNameProvider)
            : base(type)
        {
            _mappers = mappers;
            _projectionMappers = pmappers;
            _runtimeType = type;
            _client = tableService.GetTableClient(tableNameProvider.GetTableName());
        }

        private IEnumerable<string> GetSelector()
        {
            var entityPropertyConfigs = _mappers.ToPropertyConfigurations();
            var projectionPropertyConfigs = _projectionMappers.ToPropertyConfigurations();

            return entityPropertyConfigs.Where(e =>
                projectionPropertyConfigs.Select(e => e.EntityPropertyBindingName)
                                         .Any(x => e.IsConfigured(x))).Select(e => e.BindingName);
        }


        public async Task<IEnumerable<TProjection>> GetCollectionAsync(CancellationToken cancellationToken = default)
        {
            var entities = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(entities, _mappers);
            var method = CreateMethodGetAll();
            var selector = GetSelector();

            await method(this, new object[] { selector, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            return entities.ToProjections(_projectionMappers);
        }

        public async Task<IEnumerable<TProjection>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate,
                     CancellationToken cancellationToken = default)
        {
            var entities = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(entities, _mappers);
            var query = new RuntimeQueryMapper<TEntity>(predicate, _mappers.Where(e => e is ITranslateVisitorBuilderVisitor)
                .Select(e => e as ITranslateVisitorBuilderVisitor).ToList());

            var method = CreateMethodQuery();
            var filtered = GetSelector();

            await method(this, new object[] { filtered, query, mapper, _client, cancellationToken })
                .ConfigureAwait(false);

            return entities.ToProjections(_projectionMappers);
        }

        public async Task<Page<TProjection>> GetPageAsync(int pageSize = 100, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            var entities = new List<TEntity>();
            var mapper = new FromRuntimeTypeMapper<TEntity>(entities, _mappers);
            var method = CreateMethodGetPage();
            var filtered = GetSelector();
            var tokens = new List<string>();

            await method(this, new object[] { filtered, mapper, tokens, continuationToken, pageSize, _client, cancellationToken })
                .ConfigureAwait(false);

            var results = entities.ToProjections(_projectionMappers);

            return new Page<TProjection>(results, tokens.FirstOrDefault(), pageSize);
        }
    }
}
