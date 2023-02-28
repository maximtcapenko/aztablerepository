namespace AzureTableAccessor.Data
{
    using System.Collections.Generic;

    public class Page<TEntity> where TEntity : class
    {
        public Page(IEnumerable<TEntity> items, string continuationToken, int pageSize)
        {
            Items = items;
            ContinuationToken = continuationToken;
            PageSize = pageSize;
        }
        
        public string ContinuationToken { get; }

        public IEnumerable<TEntity> Items { get; }

        public int PageSize { get; }

        public bool HasNext => ContinuationToken != null;
    }
}