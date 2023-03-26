namespace AzureTableAccessor.Mappers
{
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System;
    using Infrastructure.Internal;

    internal class PropertyMapper<TEntity, TProperty> : BaseKeyPropertyMapper<TEntity, TProperty>
            where TEntity : class
    {
        private readonly MemberExpression _memberExpression;
        private readonly string _fieldName;
        private static ConcurrentDictionary<string, IMapperDelegate> _mappersCache
           = new ConcurrentDictionary<string, IMapperDelegate>();

        private static string GetKeyName<TFrom, TTo>(string property)
            => $"{typeof(TFrom).Name}-{typeof(TTo).Name}-{property}";

        public PropertyMapper(Expression<Func<TEntity, TProperty>> property)
         : base(property, PropertyConfigType.Property)
        {
            _memberExpression = property.Body as MemberExpression;
    
            var path = property.GetMemberPath();
            _fieldName = GetFiledName(path.Split('.'));
        }

        public PropertyMapper(Expression<Func<TEntity, TProperty>> property, string propertyName)
        : this(property)
        {
            _fieldName = propertyName;
        }

        private string GetFiledName(string[] names) => names.Length > 1 ? string.Join('_', names) : names[0];
        protected override string GetKeyPropertyName() => _fieldName;
    }
}
