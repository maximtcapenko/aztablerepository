namespace AzureTableAccessor.Infrastructure.Internal
{
    using System;
    using System.Collections.Generic;
    using Mappers;

    internal class RuntimeMappingConfiguration<TEntity> where TEntity : class
    {
        public RuntimeMappingConfiguration(Type runtimeType,
            IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers,
            ITableNameProvider tableNameProvider,
            IAutoKeyGenerator keyGenerator)
        {
            AutoKeyGenerator = keyGenerator;
            Mappers = mappers;
            RuntimeType = runtimeType;
            TableNameProvider = tableNameProvider;
        }

        public Type RuntimeType { get; }

        public IEnumerable<IPropertyRuntimeMapper<TEntity>> Mappers { get; }
        public ITableNameProvider TableNameProvider { get; }
        public IAutoKeyGenerator AutoKeyGenerator { get; }
    }

    internal class RuntimeMappingConfiguration<TEntity, T>
       where TEntity : class
       where T : class
    {
        public RuntimeMappingConfiguration(Type runtimeType,
            IEnumerable<IPropertyRuntimeMapper<TEntity, T>> mappers,
            ITableNameProvider tableNameProvider)
        {
            Mappers = mappers;
            RuntimeType = runtimeType;
            TableNameProvider = tableNameProvider;
        }

        public Type RuntimeType { get; }

        public IEnumerable<IPropertyRuntimeMapper<TEntity, T>> Mappers { get; }

        public ITableNameProvider TableNameProvider { get; }
    }
}