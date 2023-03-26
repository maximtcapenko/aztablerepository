namespace AzureTableAccessor.Data.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;

    internal class DefaultTransactionBuilder : ITransactionBuilder
    {
        private readonly List<(TableTransactionAction transation, Callback callback)> _transactionActions 
            = new List<(TableTransactionAction transation, Callback callback)>();

        public Func<TableClient, CancellationToken, Task> Build()
        {
            if(!_transactionActions.Any()) return null;

            if (_transactionActions.GroupBy(e => e.transation.Entity.PartitionKey).Count() > 1)
                throw new NotSupportedException("Partition key should be the same for all entities in the transaction");

            return async (client, cancellationToken) =>
            {
                var responses = await client.SubmitTransactionAsync(_transactionActions.Select(e => e.transation), cancellationToken)
                                            .ConfigureAwait(false);

                for(int i = 0; i < _transactionActions.Count; i++)
                {
                        var response = responses.Value[i];
                        _transactionActions[i].callback?.Invoke(response);
                }
            };
        }

        public ITransactionBuilder CreateEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity
        {
            _transactionActions.Add((new TableTransactionAction(TableTransactionActionType.Add, entity), callback));
            return this;
        }

        public ITransactionBuilder DeleteEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity
        {
            _transactionActions.Add((new TableTransactionAction(TableTransactionActionType.Delete, entity), callback));
            return this;
        }

        public ITransactionBuilder UpdateEntity<TEntity>(TEntity entity, Callback callback = default)
            where TEntity : class, ITableEntity
        {
            _transactionActions.Add((new TableTransactionAction(TableTransactionActionType.UpdateMerge, entity), callback));
            return this;
        }
    }
}