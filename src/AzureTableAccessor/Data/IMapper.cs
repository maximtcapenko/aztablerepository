namespace AzureTableAccessor.Data
{
    using Azure.Data.Tables;
    
    internal interface IMapper
    {
        void Map<T>(T obj) where T : class, ITableEntity, new();
    }
}