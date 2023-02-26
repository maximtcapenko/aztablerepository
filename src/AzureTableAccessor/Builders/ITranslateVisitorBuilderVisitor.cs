using Azure.Data.Tables;

namespace AzureTableAccessor.Builders
{
    internal interface ITranslateVisitorBuilderVisitor
    {
        void Visit<T>(TranslateVisitorBuilder<T> visitorBuilder) where T : class, ITableEntity;
    }
}
