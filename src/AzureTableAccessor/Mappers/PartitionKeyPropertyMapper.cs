namespace AzureTableAccessor.Mappers
{
    using System;
    using System.Linq.Expressions;

    internal class PartitionKeyPropertyMapper<TEntity, TProperty> : BaseKeyPropertyMapper<TEntity, TProperty>
        where TEntity : class
    {
        public PartitionKeyPropertyMapper(Expression<Func<TEntity, TProperty>> property)
            : base(property, PropertyConfigType.PartitionKey)
        { }

        protected override string GetKeyPropertyName() => "PartitionKey";
    }
}
