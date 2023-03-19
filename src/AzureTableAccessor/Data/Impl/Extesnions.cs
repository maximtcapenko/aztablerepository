namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
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

        internal static IEnumerable<IPropertyConfiguration<TEntity, TProjection>> ToPropertyConfigurations<TEntity, TProjection>(this IEnumerable<IPropertyRuntimeMapper<TEntity, TProjection>> mappers)
            where TEntity : class
            where TProjection : class
             => mappers.OfType<IPropertyConfigurationProvider<TEntity, TProjection>>().Select(e => e.GetPropertyConfiguration());

        internal static IEnumerable<IPropertyConfiguration<TEntity>> ToPropertyConfigurations<TEntity>(this IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
            where TEntity : class
             => mappers.OfType<IPropertyConfigurationProvider<TEntity>>().Select(e => e.GetPropertyConfiguration());

        internal static IEnumerable<TProjection> ToProjections<TEntity, TProjection>(this IEnumerable<TEntity> entities,
            IEnumerable<IPropertyRuntimeMapper<TEntity, TProjection>> projectionMappers)
            where TEntity : class
            where TProjection : class
        {
            var projections = new List<TProjection>();
            var factory = InstanceFactoryProvider.InstanceFactoryCache.GetOrAdd(typeof(TProjection),
                (t) => Expression.Lambda<Func<object>>(Expression.New(t)).Compile());


            foreach (var entity in entities)
            {
                var projection = (TProjection)factory();

                foreach (var projectionMapper in projectionMappers)
                {
                    projectionMapper.Map(entity, projection);
                }

                projections.Add(projection);
            }

            return projections;
        }
    }
}