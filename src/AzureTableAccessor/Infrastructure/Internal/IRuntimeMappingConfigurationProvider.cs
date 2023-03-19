namespace AzureTableAccessor.Infrastructure.Internal
{
    internal interface IRuntimeMappingConfigurationProvider<TEntity> where TEntity : class
    {
        RuntimeMappingConfiguration<TEntity> GetConfiguration();
    }

    internal interface IRuntimeMappingConfigurationProvider<TEntity, T> 
        where TEntity : class
        where T : class
    {
        RuntimeMappingConfiguration<TEntity, T> GetConfiguration();
    }
}