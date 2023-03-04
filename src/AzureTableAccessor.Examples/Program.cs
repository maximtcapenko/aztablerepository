namespace AzureTableAccessor.Examples
{
    using Configurators;
    using Data;
    using Infrastructure;
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
            }).ConfigureMap(typeof(Program).Assembly);


            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Message>>();

            var randomizer = new Random();
            var range = Enumerable.Range(0, 10);

            var number = "+({0}){1}{2}{3}-{4}{5}{6}{7}-{8}{9}";
            var message = new Message
            {
                ClientBillingId = Guid.NewGuid().ToString(),
                ApplicationMessageId = Guid.NewGuid().ToString(),
                Direction = MessageDirection.Outbound,
                Phone = new Phone
                {
                    Number = string.Format(number, range.OrderBy(e => randomizer.Next()).Select(e => e.ToString()).ToArray())
                },
                SmsMessageId = Guid.NewGuid().ToString(),
                Request = new OutboundSmsRequest
                {
                    Body = "hello world",
                    Receivers = new List<string> {
                        string.Format(number, range.OrderBy(e => randomizer.Next()).Select(e => e.ToString()).ToArray()),
                        string.Format(number, range.OrderBy(e => randomizer.Next()).Select(e => e.ToString()).ToArray())
                    }
                }
            };

            await repository.CreateAsync(message);

            var messages = await repository.GetCollectionAsync();
            foreach (var msg in messages)
            {
                Console.WriteLine(msg);
            }

            Console.WriteLine("-----page 0-----");
            var page = await repository.GetPageAsync(pageSize: 3);
            foreach (var msg in page.Items)
            {
                Console.WriteLine(msg);
            }
            Console.WriteLine("-----page 1-----");

            page = await repository.GetPageAsync(5, page.ContinuationToken);
            foreach (var msg in page.Items)
            {
                Console.WriteLine(msg);
            }

            var entity = messages.First();

            entity = await repository.LoadAsync(entity);
            Console.WriteLine("-----loaded-----");
            Console.WriteLine(entity);

            entity = await repository.SingleAsync(e => e.Phone.Number == "+(5)621-0843-97");
            Console.WriteLine("---find single--");
            Console.WriteLine(entity);

            message = messages.First();
            Console.WriteLine("-----delete-----");
            await repository.DeleteAsync(message);
        }
    }

    public enum MessageDirection
    {
        Inbound = 1,
        Outbound
    }

    public class OutboundSmsRequest
    {
        public bool ReturnCSVString { get; set; }

        public string ExternalLogin { get; set; }

        public string Password { get; set; }

        public string ClientBillingId { get; set; }

        public string ClientMessageId { get; set; }

        public string Originator { get; set; }

        public List<string> Receivers { get; set; }

        public string Body { get; set; }

        public long? Validity { get; set; }

        public int CharacterSetId { get; set; }

        public int ReplyMethodId { get; set; }

        public string ReplyData { get; set; }

        public string StatusNotificationUrl { get; set; }
    }

    public class Phone
    {
        public string Number { get; set; }
    }

    public class FlatMessage
    {
        public string From { get; set; }

        public string BillingId { get; set; }
        public string Phone { get; set; }

        public string SmsId { get; set; }
    }

    public class Message
    {
        public Phone Phone { get; set; }

        public string SmsMessageId { get; set; }

        public int ReplyMethodId { get; set; }

        public MessageDirection Direction { get; set; }

        public string ReplyData { get; set; }

        public string Originator { get; set; }

        public string ClientBillingId { get; set; }

        public string ApplicationMessageId { get; set; }

        public OutboundSmsRequest Request { get; set; }

        public override string ToString()
        {
            var receivers = Request == null ? "none" : string.Join(',', Request.Receivers);
            return $"id={SmsMessageId}; phone={Phone?.Number}; message body={Request?.Body} with receivers={receivers}";
        }
    }

    public class MessageTableMappingConfiguration : IMappingConfiguration<Message>
    {
        public void Configure(IMappingConfigurator<Message> configurator)
        {
            configurator.PartitionKey(e => e.ApplicationMessageId)
                        .RowKey(e => e.SmsMessageId)
                        .Property(e => e.Phone.Number)
                        .Property(e => e.ReplyData)
                        .Property(e => e.ReplyMethodId)
                        .Property(e => e.ClientBillingId)
                        .Content(e => e.Request);
        }
    }
}

