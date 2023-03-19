namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Azure.Data.Tables;
    using Mappers;

    internal class FromRuntimeTypeMapper<TEntity> : IMapper
        where TEntity : class
    {
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;

        private readonly List<TEntity> _entities;

        public FromRuntimeTypeMapper(List<TEntity> entities,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
        {
            _entities = entities;
            _mappers = mappers;
        }

        public void Map<T>(T obj) where T : class
        {
            var factory = InstanceFactoryProvider.InstanceFactoryCache.GetOrAdd(typeof(TEntity),
            (type) => Expression.Lambda<Func<object>>(Expression.New(type)).Compile());

            var entity = factory() as TEntity;

            _entities.Add(entity);

            foreach (var mapper in _mappers)
            {
                mapper.Map(obj, entity);
            }
        }
    }
}