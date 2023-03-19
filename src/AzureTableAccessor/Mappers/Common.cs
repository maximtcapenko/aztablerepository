namespace AzureTableAccessor.Mappers
{
    using System;

    internal enum PropertyConfigType
    {
        PartitionKey,
        RowKey,
        Property
    }

    internal interface IPropertyDescriber<TBuilder>
    {
        void Describe(TBuilder builder);
    }

    internal interface IPropertyConfiguration<TEntity>
    {
        PropertyConfigType PropertyConfigType { get; }
        /// <summary>
        /// Name of binding table column
        /// </summary>
        string BindingName { get; }
        string PropertyName { get; }
        Type Type { get; }
        object GetValue(TEntity entity);
        bool IsConfigured(string propertyName);
    }

    internal interface IPropertyConfiguration<TEntity, TProjection>
    {
        string EntityPropertyBindingName { get; }
        string PropertyName { get; }
    }
    
    internal interface IPropertyConfigurationProvider<TEntity>
    {
        IPropertyConfiguration<TEntity> GetPropertyConfiguration();
    }

    internal interface IPropertyConfigurationProvider<TEntity, TProjection>
    {
        IPropertyConfiguration<TEntity, TProjection> GetPropertyConfiguration();
    }
}