namespace AzureTableAccessor.Builders
{
    public interface IBuilderVisitor
    {
        void Visit(IRuntimeTypeBuilder builder);
    }
}