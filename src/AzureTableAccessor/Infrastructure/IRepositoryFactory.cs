namespace AzureTableAccessor.Infrastructure
{
    using Azure.Data.Tables;
    using Data;

    public interface IRepositoryFactory<TEntity> where TEntity : class
    {
        IRepository<TEntity> CreateRepository(TableServiceClient tableService);
    }
}