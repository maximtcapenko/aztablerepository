namespace AzureTableAccessor.Data
{
    using Azure.Data.Tables;

    internal interface IEntityCache
    {
        void Add<TEntity>(TEntity entity)
            where TEntity : class, ITableEntity;
        TEntity Get<TEntity>(string partitionKey, string rowKey)
            where TEntity : class, ITableEntity;
        
        void Remove(string partitionKey, string rowKey);
    }
}