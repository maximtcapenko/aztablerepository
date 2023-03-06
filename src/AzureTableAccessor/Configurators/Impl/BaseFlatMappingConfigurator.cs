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
    using System.Text.RegularExpressions;

    public class BaseFlatMappingConfigurator<TEntity> : IMappingConfigurator<TEntity>
                where TEntity : class
    {
        private readonly List<IPropertyDescriber<AnonymousProxyTypeBuilder>> _propertyDescribers = new List<IPropertyDescriber<AnonymousProxyTypeBuilder>>();
        private readonly AnonymousProxyTypeBuilder _typeBuilder = new AnonymousProxyTypeBuilder();
        private readonly List<bool> _keys = new List<bool>();
        private bool? _configurationIsValid;
        private readonly DefaultTableNameProvider _tableNameProvider = new DefaultTableNameProvider();

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class
        {
            property.CheckPropertyExpression();
            _propertyDescribers.Add(new ContentPropertyMapper<TEntity, TProperty>(property));

            return this;
        }

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property,
            IContentSerializer contentSerializer) where TProperty : class
        {
            property.CheckPropertyExpression();
            _propertyDescribers.Add(new ContentPropertyMapper<TEntity, TProperty>(property, contentSerializer));

            return this;
        }

        public IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _propertyDescribers.Add(new PartitionKeyPropertyMapper<TEntity, TProperty>(property));
            _keys.Add(true);

            return this;
        }

        public IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _propertyDescribers.Add(new PropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyExpression();
            property.CheckPropertyType();
            _propertyDescribers.Add(new RowKeyPropertyMapper<TEntity, TProperty>(property));
            _keys.Add(true);

            return this;
        }

        public IMappingConfigurator<TEntity> ToTable(string name)
        {
            _tableNameProvider.AddName(name);
            return this;
        }

        public IRepository<TEntity> GetRepository(TableServiceClient tableService)
        {
            ValidateConfiguration(_propertyDescribers);
            var type = _typeBuilder.CreateType(_propertyDescribers);

            return new TableClientRuntimeProxyRepository<TEntity>(tableService, type,
                _propertyDescribers.Select(e => e as IPropertyRuntimeMapper<TEntity>), _tableNameProvider);
        }

        private void ValidateConfiguration(IEnumerable<IPropertyDescriber<AnonymousProxyTypeBuilder>> builders)
        {
            if (_configurationIsValid == null)
            {
                var partitionsKeys = builders.Where(e => e.GetType().GetGenericTypeDefinition() == typeof(PartitionKeyPropertyMapper<,>));
                partitionsKeys.ValidateKeys("partition key");

                var rowKeys = builders.Where(e => e.GetType().GetGenericTypeDefinition() == typeof(RowKeyPropertyMapper<,>));
                rowKeys.ValidateKeys("row key");
                _configurationIsValid = true;
            }
        }

        internal class DefaultTableNameProvider : ITableNameProvider
        {
            private readonly List<string> _names = new List<string>();
            private const string _rulePattern = "^[A-Za-z][A-Za-z0-9]{2,62}$";

            public void AddName(string name)
            {
                _names.Add(name);
            }

            public string GetTableName()
            {
                if (!_names.Any())
                {
                    return typeof(TEntity).Name.ToLower();
                }
                var name = string.Join(null, _names);
                var match = Regex.Match(name, _rulePattern);

                if (!match.Success)
                    throw new System.ArgumentException($"Table name [{name}] is not valid");

                return name;
            }
        }
    }
}
