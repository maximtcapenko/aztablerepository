namespace AzureTableAccessor.Configurators.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Builders;
    using Extensions;
    using Infrastructure;
    using Infrastructure.Internal;
    using Mappers;

    internal class DefaultTableMappingConfigurator<TEntity> : IMappingConfigurator<TEntity>,
            IRuntimeMappingConfigurationProvider<TEntity> where TEntity : class
    {
        private readonly Dictionary<string, IBuilderVisitor> _builderVisitors = new Dictionary<string, IBuilderVisitor>();
        private readonly AnonymousTypeBuilder _typeBuilder = TypeBuilderFactory.CreateAzureTableEntity();
        private readonly List<bool> _keys = new List<bool>();
        private bool? _configurationIsValid;
        private readonly DefaultTableNameProvider<TEntity> _tableNameProvider = new DefaultTableNameProvider<TEntity>();

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property) where TProperty : class
        {
            ValidateAndAddVisitor(property, () => new ContentPropertyMapper<TEntity, TProperty>(property));
            return this;
        }

        public IMappingConfigurator<TEntity> Content<TProperty>(Expression<Func<TEntity, TProperty>> property,
            IContentSerializer contentSerializer) where TProperty : class
        {
            ValidateAndAddVisitor(property, () => new ContentPropertyMapper<TEntity, TProperty>(property, contentSerializer));
            return this;
        }

        public IMappingConfigurator<TEntity> PartitionKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyType();
            ValidateAndAddVisitor(property, () => new PartitionKeyPropertyMapper<TEntity, TProperty>(property));

            return this;
        }

        public IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyType();
            ValidateAndAddVisitor(property, () => new PropertyMapper<TEntity, TProperty>(property));

            return this;
        }

        public IMappingConfigurator<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property, string propertyName)
        {
            property.CheckPropertyType();
            ValidateAndAddVisitor(property, () => new PropertyMapper<TEntity, TProperty>(property, propertyName));

            return this;
        }

        public IMappingConfigurator<TEntity> RowKey<TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            property.CheckPropertyType();
            ValidateAndAddVisitor(property, () => new RowKeyPropertyMapper<TEntity, TProperty>(property));
            _keys.Add(true);

            return this;
        }

        public IMappingConfigurator<TEntity> ToTable(string name)
        {
            _tableNameProvider.AddName(name);
            return this;
        }

        public RuntimeMappingConfiguration<TEntity> GetConfiguration()
        {
            ValidateConfiguration(_builderVisitors.Values);
            var type = _typeBuilder.CreateType(_builderVisitors.Values);

            return new RuntimeMappingConfiguration<TEntity>(type,
                _builderVisitors.Values.Select(e => e as IPropertyRuntimeMapper<TEntity>),
                 _tableNameProvider);
        }

        private void ValidateAndAddVisitor<TProperty>(Expression<Func<TEntity, TProperty>> property,
            Func<IBuilderVisitor> factory)
        {
            var key = property.GetMemberPath();
            if (!_builderVisitors.ContainsKey(key))
            {
                property.CheckPropertyExpression();
                _builderVisitors.Add(key, factory());
            }
        }

        private void ValidateConfiguration(IEnumerable<IBuilderVisitor> builders)
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
    }
}
