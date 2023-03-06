namespace AzureTableAccessor.Infrastructure.Impl
{
    using Azure.Data.Tables;
    using Data;

    internal class DefaultTableRepositoryProvider<TEntity> : IRepositoryProvider<TEntity>
        where TEntity : class
    {
        private readonly IRepositoryFactory<TEntity> _repositoryFactory;
        private readonly TableServiceClient _tableServiceClient;

        public DefaultTableRepositoryProvider(IRepositoryFactory<TEntity> repositoryFactory,
            TableServiceClient tableServiceClient)
        {
            _repositoryFactory = repositoryFactory;
            _tableServiceClient = tableServiceClient;
        }

        public IRepository<TEntity> GetRepository() => _repositoryFactory.CreateRepository(_tableServiceClient);
    }
}
