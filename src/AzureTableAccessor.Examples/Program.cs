namespace AzureTableAccessor.Examples
{
    using Configurators;
    using Configurators.Extensions;
    using Data;
    using Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            var cfgBuilder = new ConfigurationBuilder();
            cfgBuilder.AddEnvironmentVariables();

            var config = cfgBuilder.Build();

            services.AddTableClient(options =>
            {
                options.StorageUri = config["StorageUri"];
                options.StorageAccountKey = config["StorageAccountKey"];
                options.AccountName = config["AccountName"];
            }).ConfigureMap(typeof(Program).Assembly)
              .ConfigureProjections(typeof(Program).Assembly);


            var provider = services.BuildServiceProvider();

            await BatchExample.Run(provider);
        }
    }

    public class MessageTableMappingConfiguration : IMappingConfiguration<Message>
    {
        public void Configure(IMappingConfigurator<Message> configurator)
        {
            configurator.ToTable("testmessage01")
                        .PartitionKey(e => e.ApplicationMessageId)
                        .RowKey(e => e.SmsMessageId)
                        .AutoConfigure();
        }
    }

    class MessageProjectionConfiguration : IProjectionConfiguration<Message, PhoneOnly>
    {
        public void Configure(IProjectionConfigurator<Message, PhoneOnly> configurator)
        {
            configurator.Property(e => e.Phone.Number, p => p.Number);
        }
    }
}