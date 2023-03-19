namespace AzureTableAccessor.Mappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using Infrastructure.Internal;

    internal class ProjectionPropertyMapper<TEntity, TProjection, TProperty>
        : IPropertyRuntimeMapper<TEntity, TProjection>,
        IPropertyConfigurationProvider<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
           = new ConcurrentDictionary<string, IMapperDelegate>();

        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        private readonly Expression<Func<TEntity, TProperty>> _source;
        private readonly Expression<Func<TProjection, TProperty>> _target;
        private readonly string _sourceName;
        private readonly string _targetName;

        public ProjectionPropertyMapper(Expression<Func<TEntity, TProperty>> source,
            Expression<Func<TProjection, TProperty>> target)
        {
            _source = source;
            _sourceName = source.GetMemberPath();
            _target = target;
            _targetName = target.GetMemberPath();

            _mappersCache.AddOrUpdate(GetKeyName<TEntity, TProjection>(source.GetMemberPath()),
                key =>
                {
                    var getter = MethodFactory.CreateGetter(source);
                    var setter = MethodFactory.CreateSetter(target);

                    return new MapperDelegate<TEntity, TProjection>((from, to) =>
                    {
                        var data = getter(from);
                        setter(to, data);
                    });
                }, (key, mapper) => mapper);
        }

        public void Map(TProjection from, TEntity to) => throw new NotImplementedException();

        public void Map(TEntity from, TProjection to)
        {
            if (_mappersCache.TryGetValue(GetKeyName<TEntity, TProjection>(_sourceName), out var mapper))
            {
                (mapper as MapperDelegate<TEntity, TProjection>)?.Map(from, to);
            }
        }

        public IPropertyConfiguration<TEntity, TProjection> GetPropertyConfiguration()
         => new ProjectionPropertyConfiguration<TEntity, TProjection, TProperty>
         {
             PropertyName = _targetName,
             EntityPropertyBindingName = _sourceName,
         };
    }
}