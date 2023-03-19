namespace AzureTableAccessor.Infrastructure
{
    using Data;

    public interface IRepositoryFactory
    {
        IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class;
        IRepository<TEntity, TProjection> CreateRepository<TEntity, TProjection>() 
         where TEntity : class
         where TProjection : class;
    }
}