namespace AzureTableAccessor.Mappers
{
    using System;

    internal class PropertyConfiguration<TEntity, TProperty> : IPropertyConfiguration<TEntity>
       where TEntity : class
    {
        internal Func<TEntity, TProperty> Getter { get; set; }
        internal Func<string, bool> Validator { get; set; }
        public PropertyConfigType PropertyConfigType { get; set; }

        public string BindingName { get; set; }

        public string PropertyName { get; set; }

        public Type Type { get; set; }

        public object GetValue(TEntity entity) => Getter(entity);

        public bool IsConfigured(string propertyName) => Validator(propertyName);
    }

    internal class ProjectionPropertyConfiguration<TEntity, TProjection, TProperty> : IPropertyConfiguration<TEntity, TProjection>
    {
        internal Func<string, bool> Validator { get; set; }
        public string EntityPropertyBindingName { get; set; }
        public string PropertyName { get; set; }
    }
}