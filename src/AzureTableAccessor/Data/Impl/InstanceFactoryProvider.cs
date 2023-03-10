namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Concurrent;

    internal static class InstanceFactoryProvider
    {
        public static ConcurrentDictionary<Type, Func<object>> InstanceFactoryCache
             = new ConcurrentDictionary<Type, Func<object>>();
    }
}