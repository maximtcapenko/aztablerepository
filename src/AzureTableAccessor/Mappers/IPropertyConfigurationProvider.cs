namespace AzureTableAccessor.Mappers
{
    internal interface IPropertyConfigurationProvider<TEntity>
    {
        IPropertyConfiguration<TEntity> GetPropertyConfiguration();
    }

    internal interface IPropertyConfigurationProvider<TEntity, TProjection>
    {
        IPropertyConfiguration<TEntity, TProjection> GetPropertyConfiguration();
    }
}