namespace AzureTableAccessor.Configurators
{
    public interface IProjectionConfiguration<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        void Configure(IProjectionConfigurator<TEntity, TProjection> configurator);
    }
}