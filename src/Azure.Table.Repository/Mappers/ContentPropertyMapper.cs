namespace Azure.Table.Repository.Mappers
{
    using Azure.Data.Tables;
    using Builders;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;

    internal class ContentPropertyMapper<TEntity, TProperty> :
        IPropertyRuntimeMapper<TEntity>,
        IPropertyBuilder<AnonymousProxyTypeBuilder>
        where TEntity : class
    {
        private readonly Expression<Func<TEntity, TProperty>> _property;
        private readonly string _fieldName = "Content";
        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
                = new ConcurrentDictionary<string, IMapperDelegate>();
        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        public ContentPropertyMapper(Expression<Func<TEntity, TProperty>> property)
        {
            _property = property;
        }

        public void Build(AnonymousProxyTypeBuilder builder)
        {
            builder.DefineField(_fieldName, typeof(string));
        }

        public void Map<T>(TEntity from, T to) where T : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(from, nameof(from));
            ArgumentNullException.ThrowIfNull(to, nameof(to));
            
            var mapper = _mappersCache.GetOrAdd(GetKeyName<TEntity, T>(_fieldName), (s) =>
              {
                  //build delegate for mapping
                  var getContentFunc = _property.Compile();

                  var fromparam = Expression.Parameter(typeof(string));
                  var targetparam = Expression.Parameter(typeof(T), "e");
                  var field = Expression.PropertyOrField(targetparam, _fieldName);

                  var expression = Expression.Assign(field, fromparam);

                  //cache for type
                  var func = Expression.Lambda<Action<string, T>>(expression, fromparam, targetparam).Compile();

                  return new MapperDelegate<string, T, TEntity, TProperty>(func, getContentFunc);
              });

            var contentMapper = mapper as MapperDelegate<string, T, TEntity, TProperty>;
            if (contentMapper != null)
            {
                var content = contentMapper.Content(from);
                var json = JsonConvert.SerializeObject(content);
                contentMapper.Map(json, to);
            }
        }

        public void Map<T>(T from, TEntity to) where T : class
        {
            var mapper = _mappersCache.GetOrAdd(GetKeyName<T, TEntity>(_fieldName), (s) =>
            {
                //build delegate for mapping
                var targetparam = _property.Parameters.First();
                var fromparam = Expression.Parameter(typeof(T));
                var getter = Expression.PropertyOrField(fromparam, _fieldName);
                var toParam = Expression.Parameter(typeof(TProperty));
                var getContentFunc = Expression.Lambda<Func<T, string>>(getter, fromparam).Compile();

                var expression = Expression.Assign(_property.Body, toParam);

                //cache for type
                var func = Expression.Lambda<Action<TProperty, TEntity>>(expression,
                    toParam, targetparam).Compile();

                return new MapperDelegate<TProperty, TEntity, T, string>(func, getContentFunc);
            });

            var contentMapper = mapper as MapperDelegate<TProperty, TEntity, T, string>;
            if (contentMapper != null)
            {
                var json = contentMapper.Content(from);
                if (!string.IsNullOrEmpty(json))
                {
                    var conetnt = JsonConvert.DeserializeObject<TProperty>(json);
                    contentMapper.Map(conetnt, to);
                }
            }
        }
    }
}
