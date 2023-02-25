namespace Azure.Table.Repository.Configurators.Impl
{
    using Azure.Data.Tables;
    using Builders;
    using Data;
    using Data.Impl;
    using Mappers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class BaseFlatMappingConfigurator<TEntity> : IMappingConfigurator<TEntity>
        where TEntity : class
    {
        private readonly List<IPropertyBuilder<AnonymousProxyTypeBuilder>> memberMappers = new List<IPropertyBuilder<AnonymousProxyTypeBuilder>>();
        private readonly AnonymousProxyTypeBuilder _typeBuilder;

        public BaseFlatMappingConfigurator()
        {
            _typeBuilder = new AnonymousProxyTypeBuilder();
        }

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class
        {
            CheckPropertyExpression(property);
            memberMappers.Add(new ContentPropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            CheckPropertyExpression(property);
            CheckPropertyType(property);
            memberMappers.Add(new PartitionKeyPropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            CheckPropertyExpression(property);
            CheckPropertyType(property);
            memberMappers.Add(new PropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            CheckPropertyExpression(property);
            CheckPropertyType(property);
            memberMappers.Add(new RowKeyPropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IRepository<TEntity> GetRepository(TableServiceClient tableService)
        {
            foreach (var builder in memberMappers)
                builder.Build(_typeBuilder);

            var type = _typeBuilder.CreateType();

            return new TableClientRuntimeProxyRepository<TEntity>(tableService, type, memberMappers.Select(e => e as IPropertyRuntimeMapper<TEntity>));
        }

        private void CheckPropertyExpression<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            if(!(property.Body is MemberExpression))
                throw new NotSupportedException($"Expression [{property}] must be a member access");
        }

        private void CheckPropertyType<TProperty>(Expression<Func<TEntity, TProperty>> property) 
        {
            var type = typeof(TProperty);
            if (!(type.IsPrimitive || type == typeof(string)))
                throw new NotSupportedException($"Property [{property.GetMemberPath()}] must be a string or primitive type");
        }
    }
}
