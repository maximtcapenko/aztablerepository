namespace AzureTableAccessor.Mappers
{
    using Azure.Data.Tables;
    using Builders;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal abstract class BaseKeyPropertyMapper<TEntity, TProperty> : IPropertyRuntimeMapper<TEntity>,
        IPropertyBuilder<AnonymousProxyTypeBuilder>, ITranslateVisitorBuilderVisitor,
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
                Getter = property.Compile(),
                PropertyConfigType = configType
            };
        }

        public void Build(AnonymousProxyTypeBuilder builder)
        {
            var propertyInfo = _memberExpression.Member as PropertyInfo;
            builder.DefineField(GetKeyPropertyName(), propertyInfo.PropertyType);
        }

        public void Map<T>(TEntity from, T to) where T : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(from, nameof(from));
            ArgumentNullException.ThrowIfNull(to, nameof(to));

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
            ArgumentNullException.ThrowIfNull(from, nameof(from));
            ArgumentNullException.ThrowIfNull(to, nameof(to));

            var mapper = _mappersCache.GetOrAdd(GetKeyName<T, TEntity>(GetKeyPropertyName()), (keyName) =>
           {
               //build delegate for mapping
               var targetparam = _property.Parameters.First();
               var fromparam = Expression.Parameter(typeof(T), "e");
               var field = Expression.PropertyOrField(fromparam, GetKeyPropertyName());
               var getterfunc = Expression.Lambda<Func<T, TProperty>>(field, fromparam).Compile();
               var setter = CreateSetter(_property);

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

        private Expression<Action<TEntity, TProperty>> CreateSetterInternal(Expression<Func<TEntity, TProperty>> selector)
        {
            var valueParam = Expression.Parameter(typeof(TProperty));
            var body = Expression.Assign(selector.Body, valueParam);
            return Expression.Lambda<Action<TEntity, TProperty>>(body, selector.Parameters.Single(), valueParam);
        }

        private Action<TEntity, TProperty> CreateSetter(Expression<Func<TEntity, TProperty>> selector)
        {
            var param = Expression.Parameter(typeof(TProperty));
            var tree = GetSetterExpressionTree(selector.Body as MemberExpression);

            tree.Add(Expression.Invoke(CreateSetterInternal(selector), selector.Parameters.First(), param));

            var block = Expression.Block(tree);

            return Expression.Lambda<Action<TEntity, TProperty>>(block, selector.Parameters.First(), param).Compile();
        }

        private List<Expression> GetSetterExpressionTree(MemberExpression memberExpression)
        {
            var tree = new Stack<Expression>();
            do
            {
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType != typeof(string))
                {
                    var checkExpression = Expression.Equal(memberExpression, Expression.Constant(null));
                    var expression = Expression.IfThen(checkExpression, Expression.Assign(memberExpression, Expression.MemberInit(Expression.New(propertyInfo.PropertyType))));
                    tree.Push(expression);
                }

                memberExpression = memberExpression.Expression as MemberExpression;
            }
            while (memberExpression != null);

            return tree.ToList();
        }

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
