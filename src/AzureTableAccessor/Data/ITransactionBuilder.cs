namespace AzureTableAccessor.Data
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;

    internal delegate void Callback(Azure.Response response);

    internal interface ITransactionBuilder
    {
        Func<CancellationToken, Task> Build();
        ITransactionBuilder CreateEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity;
        ITransactionBuilder UpdateEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity;
        ITransactionBuilder DeleteEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity;
    }
}