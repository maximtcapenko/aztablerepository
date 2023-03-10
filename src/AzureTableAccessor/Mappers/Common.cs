namespace AzureTableAccessor.Mappers
{
    using System;
    using Azure.Data.Tables;

    internal enum PropertyConfigType
    {
        PartitionKey,
        RowKey,
        Property,
        Content
    }

    internal interface IPropertyRuntimeMapper<TEntity> where TEntity : class
    {
        void Map<T>(TEntity from, T to) where T : class, ITableEntity;

        void Map<T>(T from, TEntity to) where T : class;
    }

    internal interface IPropertyDescriber<TBuilder>
    {
        void Describe(TBuilder builder);
    }

    internal interface IPropertyConfiguration<TEntity>
    {
        PropertyConfigType PropertyConfigType { get; }
        string Name { get; }
        Type Type { get; }
        object GetValue(TEntity entity);
    }

    internal interface IPropertyConfigurationProvider<TEntity>
    {
        IPropertyConfiguration<TEntity> GetPropertyConfiguration();
    }
}