namespace Azure.Table.Repository.Configurators
{
    using System;
    using System.Linq.Expressions;

    public interface IMappingConfigurator<TEntity> where TEntity : class
    {
        IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property);

        IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class;
    }

}
