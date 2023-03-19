namespace AzureTableAccessor.Extensions
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Azure.Data.Tables;
    using AzureTableAccessor.Data;
    using Configurators;
    using Configurators.Impl;
    using Data.Impl;
    using Infrastructure;
    using Infrastructure.Internal;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public class StorageOptions
        {
            public string AccountName { get; set; }
            public string StorageAccountKey { get; set; }
            public string StorageUri { get; set; }
        }

        public interface IMapRegistrator
        {
            IMapRegistrator Register<T>(IMappingConfiguration<T> configuration) where T : class;
        }

        public interface IProjectionRegistrator
        {
            IProjectionRegistrator Register<TEntity, TProjection>(IProjectionConfiguration<TEntity, TProjection> configuration)
             where TEntity : class
             where TProjection : class;

        }

        public interface IMappingRegistration
        {
            IProjectionRegistration ConfigureMap(Action<IMapRegistrator> configurator);
            IProjectionRegistration ConfigureMap(params Assembly[] assemblies);
        }

        public interface IProjectionRegistration
        {
            IServiceCollection ConfigureProjections(Action<IProjectionRegistrator> configurator);
            IServiceCollection ConfigureProjections(params Assembly[] assemblies);
        }

        internal class InternalMapRegistrator : IMapRegistrator
        {
            private readonly IServiceCollection _services;

            public InternalMapRegistrator(IServiceCollection services)
            {
                _services = services;
            }

            public IMapRegistrator Register<T>(IMappingConfiguration<T> configuration) where T : class
            {
                var configurator = new DefaultTableMappingConfigurator<T>();
                configuration.Configure(configurator);

                _services.AddSingleton<IRuntimeMappingConfigurationProvider<T>>(configurator);
                _services.AddSingleton<IRepositoryFactory, DefaultTableRepositoryFactory>();
                _services.AddScoped(provider => provider.GetRequiredService<IRepositoryFactory>().CreateRepository<T>());

                return this;
            }
        }

        internal class InternalProjectionRegistrator : IProjectionRegistrator
        {
            private readonly IServiceCollection _services;

            public InternalProjectionRegistrator(IServiceCollection services)
            {
                _services = services;
            }

            public IProjectionRegistrator Register<TEntity, TProjection>(IProjectionConfiguration<TEntity, TProjection> configuration)
                where TEntity : class
                where TProjection : class
            {
                var configurator = new DefaultProjectionMappingConfigurator<TEntity, TProjection>();
                configuration.Configure(configurator);

                _services.AddSingleton<IRuntimeMappingConfigurationProvider<TEntity, TProjection>>(configurator);
                _services.AddScoped<IRepository<TEntity,TProjection>>(provider => provider.GetRequiredService<IRepositoryFactory>().CreateRepository<TEntity, TProjection>());

                return this;
            }
        }

        internal class InternalProjectionRegistration : IProjectionRegistration
        {
            private readonly IServiceCollection _services;

            public InternalProjectionRegistration(IServiceCollection services)
            {
                _services = services;
            }

            public IServiceCollection ConfigureProjections(Action<IProjectionRegistrator> configurator)
            {
                var registrator = new InternalProjectionRegistrator(_services);
                configurator(registrator);

                return _services;
            }

            public IServiceCollection ConfigureProjections(params Assembly[] assemblies)
            {
                return ConfigureProjections(registrator =>
                {
                    var method = registrator.GetType().GetMethod(nameof(IProjectionRegistrator.Register));
                    foreach (var serviceType in assemblies.SelectMany(e => e.GetTypes()))
                    {
                        ReflectionUtils.DoWithGenericInterfaceImpls(serviceType, typeof(IProjectionConfiguration<,>), (@interface, implementation, name) =>
                        {
                            var genericMethod = method.MakeGenericMethod(@interface.GetGenericArguments());
                            var instance = Activator.CreateInstance(implementation);
                            genericMethod.Invoke(registrator, new object[] { instance });
                        });
                    }
                });
            }
        }

        internal class InternalMappingRegistration : IMappingRegistration
        {
            private readonly IServiceCollection _services;

            public InternalMappingRegistration(IServiceCollection services)
            {
                _services = services;
            }

            public IProjectionRegistration ConfigureMap(Action<IMapRegistrator> configuration)
            {
                var registrator = new InternalMapRegistrator(_services);
                configuration(registrator);

                return new InternalProjectionRegistration(_services);
            }

            public IProjectionRegistration ConfigureMap(params Assembly[] assemblies)
            {
                return ConfigureMap(registrator =>
                {
                    var method = registrator.GetType().GetMethod(nameof(IMapRegistrator.Register));
                    foreach (var serviceType in assemblies.SelectMany(e => e.GetTypes()))
                    {
                        ReflectionUtils.DoWithGenericInterfaceImpls(serviceType, typeof(IMappingConfiguration<>), (@interface, implementation, name) =>
                        {
                            var genericMethod = method.MakeGenericMethod(@interface.GetGenericArguments());
                            var instance = Activator.CreateInstance(implementation);
                            genericMethod.Invoke(registrator, new object[] { instance });
                        });
                    }
                });
            }
        }

        public static IMappingRegistration AddTableClient(this IServiceCollection services, Action<StorageOptions> configureOptions)
        {
            var options = new StorageOptions();
            configureOptions(options);

            var client = new TableServiceClient(
                new Uri(options.StorageUri),
                new TableSharedKeyCredential(options.AccountName, options.StorageAccountKey));

            services.AddSingleton(client);

            return new InternalMappingRegistration(services);
        }
    }
}