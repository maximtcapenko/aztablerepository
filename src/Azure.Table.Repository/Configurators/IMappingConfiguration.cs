namespace Azure.Table.Repository.Configurators
{
    public interface IMappingConfiguration<TEntity> where TEntity : class
    {
        public void Configure(IMappingConfigurator<TEntity> configurator) { }
    }
}
