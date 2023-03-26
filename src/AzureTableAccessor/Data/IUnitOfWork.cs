namespace AzureTableAccessor.Data
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUnitOfWork
    {
        IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class;
        /// <summary>
        /// Commits all transactions, all entities in the same transaction mast have the same partition key
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}