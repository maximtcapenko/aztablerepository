namespace AzureTableAccessor.Data.Impl.Mappers
{
    using System.Collections.Generic;
    using AzureTableAccessor.Mappers;

    internal class ToRuntimeTypeMapper<TEntity> : IMapper where TEntity : class
    {
        private readonly IEnumerable<IPropertyRuntimeMapper<TEntity>> _mappers;
        private readonly TEntity _entity;

        public ToRuntimeTypeMapper(TEntity entity, IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers)
        {
            _entity = entity;
            _mappers = mappers;
        }

        public void Map<T>(T obj) where T : class
        {
            foreach (var mapper in _mappers) mapper.Map(_entity, obj);
        }
    }
}