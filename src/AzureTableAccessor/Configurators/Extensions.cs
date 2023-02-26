namespace AzureTableAccessor.Configurators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using AzureTableAccessor.Builders;
    using Mappers;

    internal static class Extensions
    {
        internal static void ValidateKeys<T>(this IEnumerable<IPropertyBuilder<T>> keys, string keyType)
        {
            if (keys.Count() == 0)
                throw new NotSupportedException($"{keyType} is not configured");

            else if (keys.Count() > 1)
                throw new NotSupportedException($"{keyType} configured more then once");
        }

        internal static void CheckPropertyType<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> property)
        {
            var type = typeof(TProperty);

            if (isClass(type))
                throw new NotSupportedException($"Property [{property.GetMemberPath()}] must be a string or primitive type");

            bool isClass(Type type)
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return type.GenericTypeArguments.First().IsClass;
                    }
                    return true;
                }
                else if (type == (typeof(string))) return false;
                else
                    return type.IsClass;
            }
        }

        internal static void CheckPropertyExpression<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> property)
        {
            if (!(property.Body is MemberExpression))
                throw new NotSupportedException($"Expression [{property}] must be a member access");
        }
    }
}