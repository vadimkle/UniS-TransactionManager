using TransactionManager.Storage;
using TransactionManager.Views;

namespace TransactionManager.Services;

public class TransactionService
{
    private readonly TransactionRepository _transactionRepository;

    public TransactionService(TransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    // Получение баланса клиента
    public decimal GetBalance(Guid clientId)
    {
        return _transactionRepository.GetBalance(clientId);
    }

    // Добавление транзакции (дебет или кредит)
    public (DateTime insertDateTime, decimal clientBalance) AddTransaction(ITransaction transaction)
    {
        // Проверка существования клиента и создание, если отсутствует
        decimal currentBalance = _transactionRepository.GetBalance(transaction.ClientId);

        // Идемпотентность: проверка существующей транзакции
        if (_transactionRepository.TransactionExists(transaction.ClientId, transaction.Id))
        {
            var existingTransaction = _transactionRepository.GetTransaction(transaction.ClientId, transaction.Id);
            return (existingTransaction.DateTime, existingTransaction.Amount);
        }

        // Обновление баланса в зависимости от типа транзакции
        if (transaction is DebitTransaction)
        {
            currentBalance += transaction.Amount;
        }
        else if (transaction is CreditTransaction)
        {
            if (currentBalance < transaction.Amount)
                throw new InvalidOperationException("Insufficient funds");

            currentBalance -= transaction.Amount;
        }
        else
        {
            throw new NotImplementedException(
                $"Handling transactions of type {transaction.GetType().Name} is not implemented");
        }

        // Сохраняем транзакцию и обновляем баланс
        _transactionRepository.AddTransaction(transaction);
        _transactionRepository.UpdateBalance(transaction.ClientId, currentBalance);

        return (DateTime.UtcNow, currentBalance);
    }

    // Откат транзакции
    public (DateTime revertDateTime, decimal clientBalance) RevertTransaction(Guid transactionId, Guid clientId)
    {
        if (!_transactionRepository.TransactionExists(clientId, transactionId))
            throw new InvalidOperationException("Transaction not found");

        var transaction = _transactionRepository.GetTransaction(clientId, transactionId);
        decimal currentBalance = _transactionRepository.GetBalance(clientId);

        // Откат транзакции
        if (transaction is CreditTransaction)
        {
            currentBalance -= transaction.Amount;
        }
        else if (transaction is DebitTransaction)
        {
            currentBalance += transaction.Amount;
        }

        // Удаляем транзакцию и обновляем баланс
        _transactionRepository.RemoveTransaction(clientId, transactionId);
        _transactionRepository.UpdateBalance(clientId, currentBalance);

        return (DateTime.UtcNow, currentBalance);
    }
}
