namespace AzureTableAccessor.Data
{    
    internal interface IMapper
    {
        void Map<T>(T obj) where T : class;
    }
}