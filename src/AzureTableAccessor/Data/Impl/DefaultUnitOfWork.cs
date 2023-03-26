namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Impl.Repositories;
    using Infrastructure.Internal;
    using Microsoft.Extensions.DependencyInjection;

    internal class DefaultUnitOfWork : IUnitOfWork
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TableServiceClient _tableServiceClient;
        private readonly List<ITransactionBuilder> _transactionBuilders = new List<ITransactionBuilder>();

        public DefaultUnitOfWork(IServiceProvider serviceProvider,
            TableServiceClient tableServiceClient)
        {
            _serviceProvider = serviceProvider;
            _tableServiceClient = tableServiceClient;
        }
        public IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class
        {
            var runtimeMappingConfigurationProvider = _serviceProvider.GetRequiredService<IRuntimeMappingConfigurationProvider<TEntity>>();

            var configuration = runtimeMappingConfigurationProvider.GetConfiguration();
            var tableClient = _tableServiceClient.GetTableClient(configuration.TableNameProvider.GetTableName());

            var transactionBuilder = new DefaultTransactionBuilder(tableClient);
            _transactionBuilders.Add(transactionBuilder);

            return new TableClientRuntimeProxyRepository<TEntity>(tableClient, configuration.RuntimeType,
               configuration.Mappers, transactionBuilder, new InMemoryEntityCache());
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var transactionBuilder in _transactionBuilders)
            {
                var transaction = transactionBuilder.Build();

                if (transaction != null)
                {
                    await transaction(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}