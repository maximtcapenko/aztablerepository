namespace AzureTableAccessor.Mappers
{
    using Azure.Data.Tables;
    using Builders;
    using Infrastructure;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.Json;

    internal class ContentPropertyMapper<TEntity, TProperty> :
        IPropertyRuntimeMapper<TEntity>,
        IPropertyBuilder<AnonymousProxyTypeBuilder>
        where TEntity : class
    {
        private readonly Expression<Func<TEntity, TProperty>> _property;
        private readonly string _fieldName;
        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
                = new ConcurrentDictionary<string, IMapperDelegate>();
        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        public ContentPropertyMapper(Expression<Func<TEntity, TProperty>> property)
        {
            _property = property;
            _fieldName = $"Content_{property.GetMemberPath()}";
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
                  var getContentFunc = MethodFactory.CreateGetter(_property);

                  var fromparam = Expression.Parameter(typeof(string));
                  var targetparam = Expression.Parameter(typeof(T), "e");
                  var field = Expression.PropertyOrField(targetparam, _fieldName);
                  var expression = Expression.Assign(field, fromparam);

                  //cache for type
                  var func = Expression.Lambda<Action<string, T>>(expression, fromparam, targetparam).Compile();

                  return new MapperDelegate<TEntity, T>((from, to) =>
                  {
                      var data = getContentFunc(from);
                      var json = JsonSerializer.Serialize(data);
                      func(json, to);
                  });
              });

            (mapper as MapperDelegate<TEntity, T>)?.Map(from, to);
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
                var getContentFunc = MethodFactory.CreateGetter(Expression.Lambda<Func<T, string>>(getter, fromparam));
                var expression = Expression.Assign(_property.Body, toParam);

                //cache for type
                var func = Expression.Lambda<Action<TProperty, TEntity>>(expression,
                    toParam, targetparam).Compile();

                return new MapperDelegate<T, TEntity>((from, to) =>
                {
                    var json = getContentFunc(from);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var conetnt = JsonSerializer.Deserialize<TProperty>(json);
                        func(conetnt, to);
                    }
                });
            });

            (mapper as MapperDelegate<T, TEntity>)?.Map(from, to);
        }
    }
}
