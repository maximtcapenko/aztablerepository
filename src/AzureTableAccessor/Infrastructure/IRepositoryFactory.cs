namespace AzureTableAccessor.Infrastructure
{
    using Data;

    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates an instance of read/write repository
        /// </summary>
        IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class;
        /// <summary>
        /// Creates an instance of readonly projection repository
        /// </summary>
        IRepository<TEntity, TProjection> CreateRepository<TEntity, TProjection>() 
         where TEntity : class
         where TProjection : class;
    }
}