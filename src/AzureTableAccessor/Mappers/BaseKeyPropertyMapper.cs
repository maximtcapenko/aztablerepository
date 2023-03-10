namespace AzureTableAccessor.Mappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Azure.Data.Tables;
    using Builders;
    using Infrastructure.Internal;

    internal abstract class BaseKeyPropertyMapper<TEntity, TProperty> : IPropertyRuntimeMapper<TEntity>,
            IPropertyDescriber<AnonymousProxyTypeBuilder>, ITranslateVisitorBuilderVisitor,
            IPropertyConfigurationProvider<TEntity>
            where TEntity : class
    {
        private readonly MemberExpression _memberExpression;
        private readonly Expression<Func<TEntity, TProperty>> _property;
        private readonly InternalPropertyConfiguration _configuration;
        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
            = new ConcurrentDictionary<string, IMapperDelegate>();

        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        public BaseKeyPropertyMapper(Expression<Func<TEntity, TProperty>> property, PropertyConfigType configType)
        {
            _memberExpression = property.Body as MemberExpression;
            _property = property;
            _configuration = new InternalPropertyConfiguration
            {
                Name = property.GetMemberPath(),
                Getter = MethodFactory.CreateGetter(property),
                PropertyConfigType = configType
            };
        }

        public void Describe(AnonymousProxyTypeBuilder builder)
        {
            var propertyInfo = _memberExpression.Member as PropertyInfo;
            builder.DefineField(GetKeyPropertyName(), propertyInfo.PropertyType);
        }

        public void Map<T>(TEntity from, T to) where T : class, ITableEntity
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            var mapper = _mappersCache.GetOrAdd(GetKeyName<TEntity, T>(GetKeyPropertyName()), (keyName) =>
             {
                 //build delegate for mapping
                 var fromparam = _property.Parameters.First();
                 var targetparam = Expression.Parameter(typeof(T), "e");
                 var field = Expression.PropertyOrField(targetparam, GetKeyPropertyName());

                 var expression = Expression.Assign(field, _memberExpression);
                 var func = Expression.Lambda<Action<TEntity, T>>(expression, fromparam, targetparam).Compile();

                 return new MapperDelegate<TEntity, T>(func);
             });

            (mapper as MapperDelegate<TEntity, T>)?.Map(from, to);
        }

        public void Map<T>(T from, TEntity to) where T : class
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            var mapper = _mappersCache.GetOrAdd(GetKeyName<T, TEntity>(GetKeyPropertyName()), (keyName) =>
           {
               //build delegate for mapping
               var targetparam = _property.Parameters.First();
               var fromparam = Expression.Parameter(typeof(T), "e");
               var field = Expression.PropertyOrField(fromparam, GetKeyPropertyName());
               var getterfunc = MethodFactory.CreateGetter(Expression.Lambda<Func<T, TProperty>>(field, fromparam));
               var setter = MethodFactory.CreateSetter(_property);

               return new MapperDelegate<T, TEntity>((tfrom, tto) =>
               {
                   var data = getterfunc(tfrom);
                   setter(tto, data);
               });
           });

            (mapper as MapperDelegate<T, TEntity>)?.Map(from, to);
        }

        public void Visit<T>(TranslateVisitorBuilder<T> visitorBuilder) where T : class, ITableEntity =>
            visitorBuilder.Add(_memberExpression, Expression.PropertyOrField(visitorBuilder.ParameterExpression, GetKeyPropertyName()));

        public IPropertyConfiguration<TEntity> GetPropertyConfiguration() => _configuration;

        class InternalPropertyConfiguration : IPropertyConfiguration<TEntity>
        {
            internal Func<TEntity, TProperty> Getter { get; set; }

            public PropertyConfigType PropertyConfigType { get; set; }

            public string Name { get; set; }

            public Type Type { get; set; }

            public object GetValue(TEntity entity) => Getter(entity);
        }

        protected abstract string GetKeyPropertyName();
    }
}
