namespace AzureTableAccessor.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal class TranslateVisitor : ExpressionVisitor
    {
        private Expression _transaltedExpression;
        private readonly List<MemberVisitorFactory> _visitors = new List<MemberVisitorFactory>();
        private readonly Dictionary<Expression, Expression> _visitedEqNeqGtLtNodes = new Dictionary<Expression, Expression>();
        private readonly HashSet<Expression> _visitedOrAndNodes = new HashSet<Expression>();

        public TranslateVisitor(IEnumerable<MemberVisitorFactory> visitors)
        {
            _visitors.AddRange(visitors);
        }

        public Expression GetTranslatedExpression() => _transaltedExpression;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var operations = new Dictionary<ExpressionType, Func<Expression, Expression, Expression>>
            {
                [ExpressionType.Equal] = Expression.Equal,
                [ExpressionType.NotEqual] = Expression.NotEqual,
                [ExpressionType.GreaterThan] = Expression.GreaterThan,
                [ExpressionType.GreaterThanOrEqual] = Expression.GreaterThanOrEqual,
                [ExpressionType.LessThan] = Expression.LessThan,
                [ExpressionType.LessThanOrEqual] = Expression.LessThanOrEqual,
            };

            var groups = new Dictionary<ExpressionType, Func<Expression, Expression, Expression>>
            {
                [ExpressionType.AndAlso] = Expression.AndAlso,
                [ExpressionType.OrElse] = Expression.OrElse
            };

            if (operations.TryGetValue(node.NodeType, out var operation))
            {
                if (_visitedEqNeqGtLtNodes.ContainsKey(node)) goto exit;

                var valueVisitor = new ConstantVisitor();

                var visitors = _visitors.Select(factory => factory.Create()).ToList();

                foreach (var v in visitors)
                {
                    v.Visit(node.Left);
                    v.Visit(node.Right);
                }

                var member = visitors.Where(e => e.HasValue).Select(e => e.Value).FirstOrDefault();
                valueVisitor.Visit(node.Left);
                valueVisitor.Visit(node.Right);

                var value = valueVisitor.Value;

                if (member != null)
                {
                    _visitedEqNeqGtLtNodes[node] = operation(member, value);

                    if (_transaltedExpression == null)
                        _transaltedExpression = _visitedEqNeqGtLtNodes[node];
                }
            }

            if (groups.TryGetValue(node.NodeType, out var group))
                HandleGroup(node, group);

            exit:
            return base.VisitBinary(node);
        }

        private void HandleGroup(BinaryExpression node, Func<Expression, Expression, Expression> group)
        {
            base.Visit(node.Left);
            base.Visit(node.Right);

            var visitedInedx = _visitedEqNeqGtLtNodes.Count - 1;
            var first = _visitedEqNeqGtLtNodes.ElementAt(visitedInedx);
            var second = _visitedEqNeqGtLtNodes.ElementAt(visitedInedx - 1);

            if (_transaltedExpression == null && visitedInedx >= 0)
            {
                _transaltedExpression = group(first.Value, second.Value);
                _visitedOrAndNodes.Add(first.Key);
                _visitedOrAndNodes.Add(second.Key);
            }
            else if (visitedInedx >= 0)
            {
                if (!_visitedOrAndNodes.Contains(first.Key))
                {
                    _transaltedExpression = group(_transaltedExpression, first.Value);
                    _visitedOrAndNodes.Add(first.Key);
                }
            }
        }
    }
}