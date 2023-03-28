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
            IBuilderVisitor, ITranslateVisitorBuilderVisitor,
            IPropertyConfigurationProvider<TEntity>
            where TEntity : class
    {
        private readonly MemberExpression _memberExpression;
        private readonly Expression<Func<TEntity, TProperty>> _property;
        private readonly string _fieldName;
        private readonly Func<TEntity, TProperty> _getter;
        private readonly Action<TEntity, TProperty> _setter;
        private readonly PropertyConfigType _configType;

        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
            = new ConcurrentDictionary<string, IMapperDelegate>();

        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        public BaseKeyPropertyMapper(Expression<Func<TEntity, TProperty>> property, PropertyConfigType configType)
        {
            _fieldName = property.GetMemberPath();
            _memberExpression = property.Body as MemberExpression;
            _property = property;
            _getter = MethodFactory.CreateGetter(_property);
            _setter = MethodFactory.CreateSetter(_property);
            _configType = configType;
        }

        public void Visit(AnonymousTypeBuilder builder)
        {
            var propertyInfo = _memberExpression.Member as PropertyInfo;
            builder.DefineProperty(GetKeyPropertyName(), propertyInfo.PropertyType);
        }

        public void Map<T>(TEntity from, T to) where T : class
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            var mapper = _mappersCache.GetOrAdd(GetKeyName<TEntity, T>(GetKeyPropertyName()), (keyName) =>
             {
                 //build delegate for mapping
                 var fromParam = _property.Parameters.First();
                 var targetParam = Expression.Parameter(typeof(T), "e");
                 var field = Expression.PropertyOrField(targetParam, GetKeyPropertyName());

                 var assign = GetAssignment(field);
                 var func = Expression.Lambda<Action<TEntity, T>>(assign, fromParam, targetParam).Compile();

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
               var targetParam = _property.Parameters.First();
               var fromParam = Expression.Parameter(typeof(T), "e");
               var field = Expression.PropertyOrField(fromParam, GetKeyPropertyName());
               var getter = MethodFactory.CreateGetter(Expression.Lambda<Func<T, TProperty>>(field, fromParam));

               return new MapperDelegate<T, TEntity>((tfrom, tto) =>
               {
                   var data = getter(tfrom);
                   _setter(tto, data);
               });
           });

            (mapper as MapperDelegate<T, TEntity>)?.Map(from, to);
        }

        public void Visit<T>(TranslateVisitorBuilder<T> visitorBuilder) where T : class, ITableEntity =>
            visitorBuilder.Add(_memberExpression, Expression.PropertyOrField(visitorBuilder.ParameterExpression, GetKeyPropertyName()));

        public IPropertyConfiguration<TEntity> GetPropertyConfiguration() => new PropertyConfiguration<TEntity, TProperty>
        {
            BindingName = GetKeyPropertyName(),
            PropertyName = _fieldName,
            Getter = _getter,
            PropertyConfigType = _configType,
            Type = _memberExpression.Member.GetMemberType(),
            Validator = (propertyName) => propertyName == _fieldName
        };

        protected abstract string GetKeyPropertyName();

        protected virtual Expression GetAssignment(MemberExpression field)
             => Expression.Assign(field, _memberExpression);
    }
}
