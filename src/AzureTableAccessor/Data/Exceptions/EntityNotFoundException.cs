namespace AzureTableAccessor.Data
{
    public class EntityNotFoundException : System.Exception
    {
        public EntityNotFoundException(string partitionKey, string rowKey)
            : base($"Entity with partition key {partitionKey} and row key {rowKey} is not found")
        { }
    }
}