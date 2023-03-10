namespace AzureTableAccessor.Infrastructure.Internal
{
    internal interface IRuntimeMappingConfigurationProvider<TEntity> where TEntity : class
    {
        RuntimeMappingConfiguration<TEntity> GetConfiguration();
    }
}