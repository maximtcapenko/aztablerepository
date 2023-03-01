namespace AzureTableAccessor.Configurators.Impl
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Linq;
    using System;
    using Azure.Data.Tables;
    using Builders;
    using Data.Impl;
    using Data;
    using Infrastructure;
    using Mappers;

    public class BaseFlatMappingConfigurator<TEntity> : IMappingConfigurator<TEntity>
                where TEntity : class
    {
        private readonly List<IPropertyBuilder<AnonymousProxyTypeBuilder>> _memberMappers = new List<IPropertyBuilder<AnonymousProxyTypeBuilder>>();
        private readonly AnonymousProxyTypeBuilder _typeBuilder;
        private readonly List<bool> _keys = new List<bool>();

        public BaseFlatMappingConfigurator()
        {
            _typeBuilder = new AnonymousProxyTypeBuilder();
        }

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class
        {
            property.CheckPropertyExpression();
            _memberMappers.Add(new ContentPropertyMapper<TEntity, TProperty>(property));
            
            return this;
        }

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property,
            IContentSerializer contentSerializer) where TProperty : class
            {
                property.CheckPropertyExpression();
                _memberMappers.Add(new ContentPropertyMapper<TEntity, TProperty>(property, contentSerializer));

                return this;
            }

        public IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _memberMappers.Add(new PartitionKeyPropertyMapper<TEntity, TProperty>(property));
            _keys.Add(true);

            return this;
        }

        public IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _memberMappers.Add(new PropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _memberMappers.Add(new RowKeyPropertyMapper<TEntity, TProperty>(property));
            _keys.Add(true);

            return this;
        }

        public IRepository<TEntity> GetRepository(TableServiceClient tableService)
        {
            ValidateConfiguration(_memberMappers);

            foreach (var builder in _memberMappers)
                builder.Build(_typeBuilder);

            var type = _typeBuilder.CreateType();

            return new TableClientRuntimeProxyRepository<TEntity>(tableService, type, _memberMappers.Select(e => e as IPropertyRuntimeMapper<TEntity>));
        }

        private void ValidateConfiguration(IEnumerable<IPropertyBuilder<AnonymousProxyTypeBuilder>> builders)
        {
            var partitionsKeys = builders.Where(e => e.GetType().GetGenericTypeDefinition() == typeof(PartitionKeyPropertyMapper<,>));
            partitionsKeys.ValidateKeys("partition key");

            var rowKeys = builders.Where(e => e.GetType().GetGenericTypeDefinition() == typeof(RowKeyPropertyMapper<,>));
            rowKeys.ValidateKeys("row key");
        }
    }
}
