namespace AzureTableAccessor.Builders
{
    using System;
    using System.Linq.Expressions;

    using Helper = Infrastructure.Internal.Extensions;
    
    internal class MemberVisitor : ExpressionVisitor
    {
        public Expression Value { get; private set; }
        public bool HasValue => Value != null;

        private readonly ParameterExpression _parameter;
        private readonly MemberExpression _fromProperty;
        private readonly MemberExpression _toProperty;

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
            if (Helper.GetMemberPath(node) == Helper.GetMemberPath(_fromProperty))
            {
                var property = Expression.PropertyOrField(_parameter, Helper.GetMemberPath(_toProperty));
                Value = property;
            }

            return base.VisitMember(node);
        }
    }
}