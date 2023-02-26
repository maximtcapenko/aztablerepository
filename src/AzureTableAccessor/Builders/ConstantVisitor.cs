namespace AzureTableAccessor.Builders
{
    using System.Linq.Expressions;

    internal class ConstantVisitor : ExpressionVisitor
    {
        public Expression Value { get; private set; }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Value = node;
            return base.VisitConstant(node);
        }
    }
}
