namespace AzureTableAccessor.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public interface IRepository<TEntity> where TEntity : class
    {
        Task CreateAsync(TEntity entity);

        Task UpdateAsync(TEntity entity);

        Task DeleteAsync(TEntity entity);

        Task<TEntity> LoadAsync(TEntity entity);

        Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate);

        Task<IEnumerable<TEntity>> GetCollectionAsync();

        Task<IEnumerable<TEntity>> GetCollectionAsync(Expression<Func<TEntity, bool>> predicate);

        Task<Page<TEntity>> GetPageAsync(int pageSize = 100, string continuationToken = null);
    }
}
