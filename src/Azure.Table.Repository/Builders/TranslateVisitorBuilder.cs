namespace Azure.Table.Repository.Builders
{
    using Azure.Data.Tables;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal class TranslateVisitorBuilder<TTo>
       where TTo : class, ITableEntity
    {
        private List<MemberVisitorFactory> _factories = new List<MemberVisitorFactory>();

        public ParameterExpression ParameterExpression { get; } = Expression.Parameter(typeof(TTo));

        public void Add(MemberExpression from, MemberExpression to)
        {
            _factories.Add(new MemberVisitorFactory(() => MemberVisitor.Create(from, to, ParameterExpression)));
        }
        public TranslateVisitor Build() => new TranslateVisitor(_factories);
    }
}
