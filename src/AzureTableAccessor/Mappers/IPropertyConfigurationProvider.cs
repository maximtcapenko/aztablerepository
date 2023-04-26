namespace AzureTableAccessor.Mappers
{
    public interface IPropertyConfigurationProvider<TEntity>
    {
        IPropertyConfiguration<TEntity> GetPropertyConfiguration();
    }

    public interface IPropertyConfigurationProvider<TEntity, TProjection>
    {
        IPropertyConfiguration<TEntity, TProjection> GetPropertyConfiguration();
    }
}