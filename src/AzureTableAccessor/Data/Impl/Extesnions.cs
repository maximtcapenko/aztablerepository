namespace AzureTableAccessor.Data.Impl
{
    using System.Collections.Generic;
    using System.Linq;
    using Mappers;

    static class Extensions
    {
        internal static (object partitionKey, object rowKey) GetKeysFromEntity<TEntity>(this IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers, TEntity entity) where TEntity : class
        {
            var configurations = mappers.Select(e => e as IPropertyConfigurationProvider<TEntity>)
                                         .Where(e => e != null)
                                         .Select(e => e.GetPropertyConfiguration());

            var partitionKey = configurations.FirstOrDefault(e => e.PropertyConfigType == PropertyConfigType.PartitionKey);
            var rowKey = configurations.FirstOrDefault(e => e.PropertyConfigType == PropertyConfigType.RowKey);

            return (partitionKey?.GetValue(entity), rowKey?.GetValue(entity));
        }
    }
}