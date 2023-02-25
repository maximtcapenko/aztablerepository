namespace Azure.Table.Repository.Builders
{
    using System;
    using System.Linq.Expressions;

    internal class MemberVisitor : ExpressionVisitor
    {
        public Expression Value { get; private set; }
        public bool HasValue => Value != null;

        private ParameterExpression _parameter;
        private MemberExpression _fromProperty;
        private MemberExpression _toProperty;

        public MemberVisitor(MemberExpression fromProperty, MemberExpression toProperty, ParameterExpression parameter)
        {
            _fromProperty = fromProperty;
            _toProperty = toProperty;
            _parameter = parameter;
        }


        public static MemberVisitor Create<TFrom, TTo, TProperty>(Expression<Func<TFrom, TProperty>> from, Expression<Func<TTo, TProperty>> to, ParameterExpression parameter) =>
            new MemberVisitor(from.Body as MemberExpression, to.Body as MemberExpression, parameter);


        public static MemberVisitor Create(MemberExpression from, MemberExpression to, ParameterExpression parameter) =>
            new MemberVisitor(from, to, parameter);

        protected override Expression VisitMember(MemberExpression node)
        {
            if (Helpers.GetMemberPath(node) == Helpers.GetMemberPath(_fromProperty))
            {
                var property = Expression.PropertyOrField(_parameter, Helpers.GetMemberPath(_toProperty));
                Value = property;
            }

            return base.VisitMember(node);

        }
    }

}
