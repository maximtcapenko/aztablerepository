namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Azure.Data.Tables;
    using Builders;
    using ILocalQueryProvider = AzureTableAccessor.Data.IQueryProvider;

    internal class RuntimeQueryMapper<TEntity> : ILocalQueryProvider where TEntity : class
    {
        private readonly Expression<Func<TEntity, bool>> _predicate;
        private readonly IEnumerable<ITranslateVisitorBuilderVisitor> _builderVisitors;

        public RuntimeQueryMapper(Expression<Func<TEntity, bool>> predicate,
            IEnumerable<ITranslateVisitorBuilderVisitor> builderVisitors)
        {
            _builderVisitors = builderVisitors;
            _predicate = predicate;
        }

        public Expression<Func<T, bool>> Query<T>() where T : class, ITableEntity, new()
        {
            var builder = new TranslateVisitorBuilder<T>();
            foreach (var visitor in _builderVisitors) visitor.Visit(builder);

            var translator = builder.Build();
            translator.Visit(_predicate);
            var translation = translator.GetTranslatedExpression();
            if(translation == null)
                throw new NotSupportedException("Can't build predicate expression");

            //build lambda
            var predicate = Expression.Lambda<Func<T, bool>>(translation, builder.ParameterExpression);
            return predicate;
        }
    }
}