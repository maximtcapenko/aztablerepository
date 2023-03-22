namespace AzureTableAccessor.Builders
{
    using Azure.Data.Tables;

    internal class TypeBuilderFactory
    {
        public static AnonymousTypeBuilder CreateAzureTableEntity() => new AnonymousTypeBuilder(typeof(ITableEntity));
    }
}