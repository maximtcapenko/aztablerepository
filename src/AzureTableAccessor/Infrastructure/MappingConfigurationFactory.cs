namespace AzureTableAccessor.Infrastructure
{
    using System.Linq;
    using System.Reflection;
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Azure.Data.Tables;
    using Configurators.Impl;
    using Configurators;
    using Data;

    public interface IRepositoryFactory<TEntity> where TEntity : class
    {
        IRepository<TEntity> Create();
    }

    internal class DefaultRepositoryFactory<TEntity> : IRepositoryFactory<TEntity>
        where TEntity : class
    {
        private readonly BaseFlatMappingConfigurator<TEntity> _configurator;
        private readonly TableServiceClient _tableServiceClient;

        public DefaultRepositoryFactory(BaseFlatMappingConfigurator<TEntity> configurator,
            TableServiceClient tableServiceClient)
        {
            _configurator = configurator;
            _tableServiceClient = tableServiceClient;
        }

        public IRepository<TEntity> Create() => _configurator.GetRepository(_tableServiceClient);
    }

    public static class MappingRegistrationExtensions
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
                _services.AddSingleton<IRepositoryFactory<T>>(provider =>
                {
                    var configurator = new BaseFlatMappingConfigurator<T>();
                    configuration.Configure(configurator);

                    return new DefaultRepositoryFactory<T>(configurator, provider.GetRequiredService<TableServiceClient>());
                });

                _services.AddScoped(provider => provider.GetRequiredService<IRepositoryFactory<T>>().Create());

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
                        ReflectionUtils.ProcessGenericInterfaceImpls(serviceType, typeof(IMappingConfiguration<>), (@interface, implementation, name) =>
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
