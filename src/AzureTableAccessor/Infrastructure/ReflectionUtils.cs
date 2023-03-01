namespace AzureTableAccessor.Infrastructure
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal static class ReflectionUtils
    {
        internal static MethodInfo FindNonPublicGenericMethod(this Type type, string name)
            => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                   .FirstOrDefault(m => m.IsGenericMethod && m.Name == name);

        internal static MethodInfo FindNonPublicGenericMethod(this Type type, string name, int paramCount)
            => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.GetParameters().Length == paramCount && m.IsGenericMethod && m.Name == name);


        internal static void ProcessGenericInterfaceImpls(Type serviceType, Type interfaceType, Action<Type, Type, string> action)
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
    }
}