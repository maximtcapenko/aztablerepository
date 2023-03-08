namespace AzureTableAccessor.Configurators.Extensions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Infrastructure;

    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Applies auto configuration of properties using convention.
        /// If the type is a string or primitive, it'll be configured as a Property. Otherwise, it'll be configured as a Content.
        /// Already configured properties will be ignored.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="configurator">Mapping configurator</param>
        public static IMappingConfigurator<TEntity> AutoConfigure<TEntity>(this IMappingConfigurator<TEntity> configurator)
            where TEntity : class
        {
            var type = typeof(TEntity);
            var propertyConfigureInfo = typeof(ConfigurationExtensions).GetMethod(nameof(ConfigureProperty),
                 BindingFlags.Static | BindingFlags.NonPublic);
            var contentConfigureInfo = typeof(ConfigurationExtensions).GetMethod(nameof(ConfigureContent),
                BindingFlags.Static | BindingFlags.NonPublic);

            ReflectionUtils.DoWithPulicProperties(type, info =>
            {
                // apply convention if property type is string or primitive then call ConfigureProperty
                // else call ConfigureContent

                if (ReflectionUtils.IsPrimitive(info.PropertyType))
                {
                    var propertyConfigureMethod = propertyConfigureInfo.MakeGenericMethod(type, info.PropertyType);
                    propertyConfigureMethod.Invoke(null, new object[] { configurator, Expression.Parameter(type), info });
                }
                else
                {
                    var contentConfigureMethod = contentConfigureInfo.MakeGenericMethod(type, info.PropertyType);
                    contentConfigureMethod.Invoke(null, new object[] { configurator, Expression.Parameter(type), info });
                }
            });

            return configurator;
        }

        internal static void ConfigureProperty<TEntity, TProperty>(IMappingConfigurator<TEntity> configurator, ParameterExpression instance,
            PropertyInfo info)
            where TEntity : class
        {
            var expression = Expression.Lambda<Func<TEntity, TProperty>>(Expression.Property(instance, info), instance);
            configurator.Property(expression);
        }

        internal static void ConfigureContent<TEntity, TProperty>(IMappingConfigurator<TEntity> configurator, ParameterExpression instance,
            PropertyInfo info)
            where TEntity : class
            where TProperty : class
        {
            var expression = Expression.Lambda<Func<TEntity, TProperty>>(Expression.Property(instance, info), instance);
            configurator.Content(expression);
        }
    }
}