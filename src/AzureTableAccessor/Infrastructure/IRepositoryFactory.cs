namespace AzureTableAccessor.Infrastructure
{
    using Data;

    public interface IRepositoryFactory
    {
        IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class;
    }
}