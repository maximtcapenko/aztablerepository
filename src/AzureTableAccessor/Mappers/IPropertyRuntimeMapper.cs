namespace AzureTableAccessor.Mappers
{
    
    public interface IPropertyRuntimeMapper<TEntity> where TEntity : class
    {
        void Map<T>(TEntity from, T to) where T : class;

        void Map<T>(T from, TEntity to) where T : class;
    }

    public interface IPropertyRuntimeMapper<TEntity, T> 
        where TEntity : class
        where T : class
    {
        void Map(TEntity from, T to);

        void Map(T from, TEntity to);
    }
}