namespace AzureTableAccessor.Infrastructure
{
    public interface IContentSerializer
    {
        string Serialize<TValue>(TValue value);
        TValue Deserialize<TValue>(string value);
    }
}