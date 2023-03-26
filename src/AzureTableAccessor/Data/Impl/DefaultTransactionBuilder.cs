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

        private readonly TableClient _tableClient;

        public DefaultTransactionBuilder(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public Func<CancellationToken, Task> Build()
        {
            if(!_transactionActions.Any()) return null;

            if (_transactionActions.GroupBy(e => e.transation.Entity.PartitionKey).Count() > 1)
                throw new NotSupportedException("The partition key must be the same for all objects in a transaction");

            return async (cancellationToken) =>
            {
                var responses = await _tableClient.SubmitTransactionAsync(_transactionActions.Select(e => e.transation), cancellationToken)
                                            .ConfigureAwait(false);

                for(int i = 0; i < _transactionActions.Count; i++)
                {
                        var response = responses.Value[i];
                        _transactionActions[i].callback?.Invoke(response);
                }

                _transactionActions.Clear();
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