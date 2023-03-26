namespace AzureTableAccessor.Examples
{
    class Generator
    {
        static Random randomizer = new Random();

        public static Message Generate(string partitionKey = default)
        {
            var range = Enumerable.Range(0, 10);

            var number = "+({0}){1}{2}{3}-{4}{5}{6}{7}-{8}{9}";
            if(string.IsNullOrEmpty(partitionKey))
                partitionKey = Guid.NewGuid().ToString();

            return new Message
            {
                ClientBillingId = Guid.NewGuid().ToString(),
                ApplicationMessageId = partitionKey,
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
        }
    }
}