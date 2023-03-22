namespace AzureTableAccessor.Builders
{
    internal interface IBuilderVisitor
    {
        void Visit(AnonymousTypeBuilder builder);
    }
}