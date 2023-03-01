namespace AzureTableAccessor.Configurators
{
    public interface IMappingConfiguration<TEntity> where TEntity : class
    {
         void Configure(IMappingConfigurator<TEntity> configurator);
    }
}
