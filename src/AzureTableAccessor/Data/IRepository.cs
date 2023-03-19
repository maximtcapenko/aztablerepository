namespace AzureTableAccessor.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRepository<TEntity> where TEntity : class
    {
        Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity> LoadAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetCollectionAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Page<TEntity>> GetPageAsync(int pageSize = 100, string continuationToken = null, CancellationToken cancellationToken = default);
    }

    public interface IRepository<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        Task<IEnumerable<TProjection>> GetCollectionAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<TProjection>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Page<TProjection>> GetPageAsync(int pageSize = 100, string continuationToken = null, CancellationToken cancellationToken = default);
     }
}
