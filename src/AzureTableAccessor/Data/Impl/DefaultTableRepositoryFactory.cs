namespace AzureTableAccessor.Data.Impl
{
    using System;
    using Azure.Data.Tables;
    using Data;
    using Impl.Repositories;
    using Infrastructure;
    using Infrastructure.Internal;
    using Microsoft.Extensions.DependencyInjection;

    internal class DefaultTableRepositoryFactory : IRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultTableRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class
        {
            var tableServiceClient = _serviceProvider.GetRequiredService<TableServiceClient>();
            var runtimeMappingConfigurationProvider = _serviceProvider.GetRequiredService<IRuntimeMappingConfigurationProvider<TEntity>>();

            var configuration = runtimeMappingConfigurationProvider.GetConfiguration();
            var tablieClient = tableServiceClient.GetTableClient(configuration.TableNameProvider.GetTableName());

            return new TableClientRuntimeProxyRepository<TEntity>(tablieClient, configuration.RuntimeType,
                 configuration.Mappers, new InMemoryEntityCache());
        }

        public IRepository<TEntity, TProjection> CreateRepository<TEntity, TProjection>()
            where TEntity : class
            where TProjection : class
        {
            var tableServiceClient = _serviceProvider.GetRequiredService<TableServiceClient>();
            var runtimeMappingConfigurationProvider = _serviceProvider.GetRequiredService<IRuntimeMappingConfigurationProvider<TEntity>>();
            var projectionConfigurationProvider = _serviceProvider.GetRequiredService<IRuntimeMappingConfigurationProvider<TEntity, TProjection>>();

            var configuration = runtimeMappingConfigurationProvider.GetConfiguration();
            var projectionConfiguration = projectionConfigurationProvider.GetConfiguration();

            return new TableClientRuntimeProxyProjectionRepository<TEntity, TProjection>(tableServiceClient, configuration.RuntimeType,
               configuration.Mappers, projectionConfiguration.Mappers,
               configuration.TableNameProvider, new InMemoryEntityCache());
        }
    }
}
