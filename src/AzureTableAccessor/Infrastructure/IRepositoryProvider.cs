namespace AzureTableAccessor.Infrastructure
{
    using Data;

    public interface IRepositoryProvider<TEntity> where TEntity : class
    {
        IRepository<TEntity> GetRepository();
    }
}