namespace AzureTableAccessor.Configurators
{
    using System;
    using System.Linq.Expressions;
    using Infrastructure;

    public interface IMappingConfigurator<TEntity> where TEntity : class
    {
        /// <summary>
        /// Binds the entity to an existing table or create a new table with the given name
        /// </summary>
        /// <param name="name">Table name</param>
        IMappingConfigurator<TEntity> ToTable(string name);
        /// <summary>
        /// Binds the property to table partition key
        /// </summary>
        /// <param name="property">Property expression</param>
        IMappingConfigurator<TEntity> PartitionKey(Expression<Func<TEntity, string>> property);
        /// <summary>
        /// Binds the property to table partition key and configure auto key generation
        /// </summary>
        /// <param name="property">Property expression</param>
        /// <param name="generator">Auto key generator</param>
        IMappingConfigurator<TEntity> PartitionKey(Expression<Func<TEntity, string>> property, IAutoKeyGenerator generator);
        /// <summary>
        /// Binds the property to table row key
        /// </summary>
        /// <param name="property">Property expression</param>
        IMappingConfigurator<TEntity> RowKey(Expression<Func<TEntity, string>> property);
        /// <summary>
        /// Binds the property to table column with the same name
        /// </summary>
        /// <param name="property">Property expression</param>
        IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property);
        /// <summary>
        /// Binds the property to table column with the given name
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="property">Property expression</param>
        /// <param name="propertyName">Custom column name</param>
        IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property, string propertyName);
        /// <summary>
        /// Binds the property to table columns with the same name and sets the property to be saved as a serialized string, defaulting to json
        /// </summary>
        /// <param name="property">Property expression</param>
        IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class;
        /// <summary>
        /// Binds the property to table columns with the same name and sets the property to be saved as a serialized string, serialized with custom serializer
        /// </summary>
        /// <param name="property">Property expression</param>
        /// <param name="contentSerializer">Custom serializer</param>
        IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property,
            IContentSerializer contentSerializer) where TProperty : class;
    }
}
