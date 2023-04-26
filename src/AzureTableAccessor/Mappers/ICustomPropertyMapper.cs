namespace AzureTableAccessor.Mappers
{
    using System;
    using System.Linq.Expressions;
    using Builders;

    public interface ICustomPropertyMapper<TEntity, TProperty> : IPropertyRuntimeMapper<TEntity>,
            IBuilderVisitor,
            IPropertyConfigurationProvider<TEntity>
            where TEntity : class
    {
         Expression<Func<TEntity, TProperty>> GetProperty();
    }
}