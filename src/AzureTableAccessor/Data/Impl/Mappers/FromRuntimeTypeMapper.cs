namespace AzureTableAccessor.Data.Impl.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using AzureTableAccessor.Mappers;

    internal class FromRuntimeTypeMapper<TEntity> : IMapper
        where TEntity : class
    {
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly List<TEntity> _entities;
        private readonly TEntity _entity;

        public FromRuntimeTypeMapper(List<TEntity> entities,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
        {
            _entities = entities;
            _mappers = mappers;
        }

        public FromRuntimeTypeMapper(TEntity entity,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
        {
            _entity = entity;
            _mappers = mappers;
        }

        public void Map<T>(T obj) where T : class
        {
            TEntity entity = _entity;

            if (entity == null)
            {
                var factory = InstanceFactoryProvider.InstanceFactoryCache.GetOrAdd(typeof(TEntity),
                (type) => Expression.Lambda<Func<object>>(Expression.New(type)).Compile());

                entity = factory() as TEntity;
                _entities.Add(entity);
            }

            foreach (var mapper in _mappers)
            {
                mapper.Map(obj, entity);
            }
        }
    }
}