namespace AzureTableAccessor.Data.Impl
{
    using System.Collections.Generic;
    using Azure.Data.Tables;

    internal class InMemoryEntityCache : IEntityCache
    {
        private readonly Dictionary<string, object> _internalTableCache = new Dictionary<string, object>();
        private const string _cacheKeyPattern = "{0}-{1}";

        public void Add<TEntity>(TEntity entity)
            where TEntity : class, ITableEntity
        {
            var key = string.Format(_cacheKeyPattern, entity.PartitionKey, entity.RowKey);
            _internalTableCache[key] = entity;
        }

        public TEntity Get<TEntity>(string partitionKey, string rowKey)
            where TEntity : class, ITableEntity
        {
            var key = string.Format(_cacheKeyPattern, partitionKey, rowKey);
            if (_internalTableCache.TryGetValue(key, out var entity))
                return (TEntity)entity;

            return null;
        }

        public void Remove(string partitionKey, string rowKey)
        {
            var key = string.Format(_cacheKeyPattern, partitionKey, rowKey);

            if (_internalTableCache.ContainsKey(key))
            {
                _internalTableCache.Remove(key);
            }
        }
    }
}