namespace AzureTableAccessor.Mappers
{
    using System;
    using System.Linq.Expressions;

    internal class RowKeyPropertyMapper<TEntity, TProperty> : BaseKeyPropertyMapper<TEntity, TProperty>
        where TEntity : class
    {
        public RowKeyPropertyMapper(Expression<Func<TEntity, TProperty>> property) : base(property)
        { }

        protected override string GetKeyPropertyName() => "RowKey";
    }
}
