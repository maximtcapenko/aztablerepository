namespace AzureTableAccessor.Infrastructure.Internal
{
    using System;
    using System.Collections.Generic;
    using Mappers;

    internal class RuntimeMappingConfiguration<TEntity> where TEntity : class
    {
        public RuntimeMappingConfiguration(Type runtimeType, IEnumerable<IPropertyRuntimeMapper<TEntity>> mappers,
            ITableNameProvider tableNameProvider)
        {
            Mappers = mappers;
            RuntimeType = runtimeType;
            TableNameProvider = tableNameProvider;
        }

        public Type RuntimeType { get; }

        public IEnumerable<IPropertyRuntimeMapper<TEntity>> Mappers { get; }

        public ITableNameProvider TableNameProvider { get; }
    }
}