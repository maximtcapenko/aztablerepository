namespace AzureTableAccessor.Examples
{
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

    public class PhoneOnly
    {
        public string Number { get; set; }
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
}