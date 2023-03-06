namespace AzureTableAccessor.Configurators
{
    using System;
    using System.Linq.Expressions;
    using Infrastructure;

    public interface IMappingConfigurator<TEntity> where TEntity : class
    {
        IMappingConfigurator<TEntity> ToTable(string name);
        
        IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class;
        
        IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property,
            IContentSerializer contentSerializer) where TProperty : class;
    }
}
