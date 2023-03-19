namespace AzureTableAccessor.Infrastructure.Internal
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal static class ReflectionUtils
    {
        private static MethodInfo FindNonPublicGenericMethod(Type type, Func<MethodInfo, bool> predicate)
        {
            var current = type;
            do
            {
                var method = current.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                      .FirstOrDefault(m => m.IsGenericMethod && predicate(m));
                if (method != null)
                    return method;

                current = current.BaseType;
            } while (current != null);

            return null;
        }

        internal static MethodInfo FindNonPublicGenericMethod(this Type type, string name)
            => FindNonPublicGenericMethod(type, m => m.Name == name);

        internal static MethodInfo FindNonPublicGenericMethod(this Type type, string name, int paramCount)
            => FindNonPublicGenericMethod(type, m => m.GetParameters().Length == paramCount && m.Name == name);

        internal static void DoWithGenericInterfaceImpls(Type serviceType, Type interfaceType, Action<Type, Type, string> action)
        {
            if (serviceType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType))
            {
                var types = serviceType.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType);

                foreach (var type in types)
                {
                    action(type, serviceType, null);
                }
            }
        }

        internal static void DoWithPulicProperties(Type type, Action<PropertyInfo> action)
        {
            foreach (var property in type.GetProperties())
            {
                action(property);
            }
        }

        internal static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string) || type.IsEnum;

        internal static bool isClass(Type type)
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
}