namespace AzureTableAccessor.Data
{
    using System;
    using System.Linq.Expressions;
    using Azure.Data.Tables;
    
    internal interface IQueryProvider
    {
        Expression<Func<T, bool>> Query<T>() where T : class, ITableEntity, new();
    }
}