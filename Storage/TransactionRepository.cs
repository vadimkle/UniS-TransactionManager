using TransactionManager.Views;

namespace TransactionManager.Storage
{
    public class TransactionRepository
    {
        // Хранилище транзакций: клиент -> транзакции
        private readonly Dictionary<Guid, Dictionary<Guid, ITransaction>> _clientTransactions = new();

        // Хранилище балансов клиентов: клиент -> баланс
        private readonly Dictionary<Guid, decimal> _clientBalances = new();

        // Получение баланса клиента
        public decimal GetBalance(Guid clientId)
        {
            return _clientBalances.ContainsKey(clientId) ? _clientBalances[clientId] : 0;
        }

        // Добавление транзакции клиенту
        public void AddTransaction(ITransaction transaction)
        {
            if (!_clientTransactions.ContainsKey(transaction.ClientId))
            {
                _clientTransactions[transaction.ClientId] = new Dictionary<Guid, ITransaction>();
                _clientBalances[transaction.ClientId] = 0;
            }

            var transactions = _clientTransactions[transaction.ClientId];
            transactions[transaction.Id] = transaction;
        }

        // Проверка существования транзакции
        public bool TransactionExists(Guid clientId, Guid transactionId)
        {
            return _clientTransactions.ContainsKey(clientId) && _clientTransactions[clientId].ContainsKey(transactionId);
        }

        // Получение транзакции по клиенту и ID транзакции
        public ITransaction GetTransaction(Guid clientId, Guid transactionId)
        {
            if (TransactionExists(clientId, transactionId))
            {
                return _clientTransactions[clientId][transactionId];
            }

            throw new InvalidOperationException("Transaction not found");
        }

        // Удаление транзакции
        public void RemoveTransaction(Guid clientId, Guid transactionId)
        {
            if (_clientTransactions.ContainsKey(clientId))
            {
                _clientTransactions[clientId].Remove(transactionId);
            }
        }

        // Обновление баланса клиента
        public void UpdateBalance(Guid clientId, decimal amount)
        {
            _clientBalances[clientId] = amount;
        }
    }
}
