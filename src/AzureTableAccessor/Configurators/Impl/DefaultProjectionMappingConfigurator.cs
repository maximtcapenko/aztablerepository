namespace AzureTableAccessor.Configurators.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Infrastructure.Internal;
    using Mappers;

    internal class DefaultProjectionMappingConfigurator<TEntity, TProjection>
        : IRuntimeMappingConfigurationProvider<TEntity, TProjection>,
          IProjectionConfigurator<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
        private readonly Dictionary<string, IPropertyRuntimeMapper<TEntity, TProjection>> _mappers
            = new Dictionary<string, IPropertyRuntimeMapper<TEntity, TProjection>>();

        public RuntimeMappingConfiguration<TEntity,TProjection> GetConfiguration()
        {
            return new RuntimeMappingConfiguration<TEntity, TProjection>(typeof(TProjection), _mappers.Values, null);
        }

        public IProjectionConfigurator<TEntity, TProjection> Property<TProperty>(Expression<Func<TEntity, TProperty>> source, Expression<Func<TProjection, TProperty>> target)
        {
            _mappers[target.GetMemberPath()] = new ProjectionPropertyMapper<TEntity, TProjection, TProperty>(source, target);
            return this;
        }
    }
}