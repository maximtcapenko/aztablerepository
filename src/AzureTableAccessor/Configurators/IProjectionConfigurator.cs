namespace AzureTableAccessor.Configurators
{
    using System;
    using System.Linq.Expressions;
    
    public interface IProjectionConfigurator<TEntity, TProjection>
        where TEntity : class
        where TProjection : class
    {
         IProjectionConfigurator<TEntity, TProjection> Property<TProperty>(Expression<Func<TEntity, TProperty>> source,
            Expression<Func<TProjection, TProperty>> target);
    }
}