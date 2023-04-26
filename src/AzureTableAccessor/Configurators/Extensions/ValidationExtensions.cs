namespace AzureTableAccessor.Configurators.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Internal;
    using Builders;
    using Exceptions;

    public static class ValidationExtensions
    {
        public static void ValidateKeys(this IEnumerable<IBuilderVisitor> keys, string keyType)
        {
            if (keys.Count() == 0)
                throw new PropertyConfigurationException($"{keyType} is not configured");

            else if (keys.Count() > 1)
                throw new PropertyConfigurationException($"{keyType} configured more then once");
        }

        public static void CheckPropertyType<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> property)
        {
            var type = typeof(TProperty);

            if (ReflectionUtils.isClass(type))
                throw new PropertyConfigurationException($"Property [{property.GetMemberPath()}] must be a string or primitive type");
        }

        public static void CheckPropertyExpression<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> property)
        {
            if (!(property.Body is MemberExpression))
                throw new PropertyConfigurationException($"Expression [{property}] must be a member access");
        }
    }
}