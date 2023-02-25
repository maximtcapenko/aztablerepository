using Azure.Data.Tables;

namespace Azure.Table.Repository.Builders
{
    internal interface ITranslateVisitorBuilderVisitor
    {
        void Visit<T>(TranslateVisitorBuilder<T> visitorBuilder) where T : class, ITableEntity;
    }
}
