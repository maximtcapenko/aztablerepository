namespace AzureTableAccessor.Extensions
{
    using System.Linq;
    using System.Reflection;
    using System;
    using Configurators;
    using Microsoft.Extensions.DependencyInjection;
    using Configurators.Impl;
    using Infrastructure;
    using Infrastructure.Impl;
    using Azure.Data.Tables;

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

        public interface IMappingRegistration
        {
            IServiceCollection ConfigureMap(Action<IMapRegistrator> configurator);
            IServiceCollection ConfigureMap(params Assembly[] assemblies);
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

                _services.AddSingleton<IRepositoryFactory<T>>(configurator);
                _services.AddSingleton<IRepositoryProvider<T>, DefaultTableRepositoryProvider<T>>();
                _services.AddScoped(provider => provider.GetRequiredService<IRepositoryProvider<T>>().GetRepository());

                return this;
            }
        }

        internal class InternalMappingRegistration : IMappingRegistration
        {
            private readonly IServiceCollection _services;

            public InternalMappingRegistration(IServiceCollection services)
            {
                _services = services;
            }

            public IServiceCollection ConfigureMap(Action<IMapRegistrator> configuration)
            {
                var registrator = new InternalMapRegistrator(_services);
                configuration(registrator);

                return _services;
            }

            public IServiceCollection ConfigureMap(params Assembly[] assemblies)
            {
                ConfigureMap(registrator =>
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

                return _services;
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