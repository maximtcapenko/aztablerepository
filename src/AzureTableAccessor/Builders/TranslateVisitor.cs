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
            var dict = new Dictionary<ExpressionType, Func<Expression, Expression, Expression>>
            {
                [ExpressionType.Equal] = Expression.Equal,
                [ExpressionType.NotEqual] = Expression.NotEqual,
                [ExpressionType.GreaterThan] = Expression.GreaterThan,
                [ExpressionType.GreaterThanOrEqual] = Expression.GreaterThanOrEqual,
                [ExpressionType.LessThan] = Expression.LessThan,
                [ExpressionType.LessThanOrEqual] = Expression.LessThanOrEqual,
            };

            var gdict = new Dictionary<ExpressionType, Func<Expression, Expression, Expression>>
            {
                [ExpressionType.AndAlso] = Expression.AndAlso,
                [ExpressionType.OrElse] = Expression.OrElse
            };

            if (dict.TryGetValue(node.NodeType, out var func))
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

                _visitedEqNeqGtLtNodes[node] = func(member, value);
                if (_transaltedExpression == null)
                    _transaltedExpression = _visitedEqNeqGtLtNodes[node];
            }


            void processGroup(BinaryExpression binary, Func<Expression, Expression, Expression> func)
            {
                base.Visit(node.Left);
                base.Visit(node.Right);

                var visitedInedx = _visitedEqNeqGtLtNodes.Count - 1;
                var first = _visitedEqNeqGtLtNodes.ElementAt(visitedInedx);
                var second = _visitedEqNeqGtLtNodes.ElementAt(visitedInedx - 1);

                if (_transaltedExpression == null && visitedInedx >= 0)
                {
                    _transaltedExpression = func(first.Value, second.Value);
                    _visitedOrAndNodes.Add(first.Key);
                    _visitedOrAndNodes.Add(second.Key);
                }
                else if (visitedInedx >= 0)
                {
                    if (!_visitedOrAndNodes.Contains(first.Key))
                    {
                        _transaltedExpression = func(_transaltedExpression, first.Value);
                        _visitedOrAndNodes.Add(first.Key);
                    }
                }
            }

            if (gdict.TryGetValue(node.NodeType, out var gfunc)) processGroup(node, gfunc);

            exit:
            return base.VisitBinary(node);
        }
    }
}