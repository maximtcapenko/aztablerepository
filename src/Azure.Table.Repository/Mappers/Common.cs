namespace Azure.Table.Repository.Mappers
{
    using Azure.Data.Tables;

    internal interface IPropertyRuntimeMapper<TEntity> where TEntity : class
    {
        void Map<T>(TEntity from, T to) where T : class, ITableEntity;

        void Map<T>(T from, TEntity to) where T : class;
    }

    internal interface IPropertyBuilder<TBuilder>
    {
        void Build(TBuilder builder);
    }

}