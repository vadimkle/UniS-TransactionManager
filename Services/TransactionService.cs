using AutoMapper;
using TransactionManager.Data;
using TransactionManager.Data.Models;
using TransactionManager.Dtos;
using TransactionManager.Exceptions;

namespace TransactionManager.Services;

public interface ITransactionService
{
    Task<TimeAndMoneyDto> GetClientBalanceAsync(Guid clientId);
    Task<TimeAndMoneyDto> AddTransactionAsync(TransactionDto transaction);
    Task<TimeAndMoneyDto> RevertTransactionAsync(Guid transactionId, Guid clientId);
}

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IMapper _mapper;

    public TransactionService(ITransactionRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<TimeAndMoneyDto> GetClientBalanceAsync(Guid clientId)
    {
        var client = await _repository.GetClientByIdAsync(clientId);

        if (client == null)
        {
            throw new KeyNotFoundException($"Client not found. Client ID: {clientId}.");
        }

        return new TimeAndMoneyDto
        { BalanceDateTime = client.LastUpdated, Balance = client.Balance };
    }

    public async Task<TimeAndMoneyDto> AddTransactionAsync(TransactionDto transaction)
    {
        var existingTransaction = await _repository.GetTransactionByIdAsync(transaction.TransactionId);
        if (existingTransaction != null)
        {
            return new TimeAndMoneyDto(existingTransaction.CreatedDateUtc, existingTransaction.ClientBalance);
        }

        var client = await _repository.GetClientByIdAsync(transaction.ClientId);

        ValidateTime(transaction.DateTime, client);
        ValidateBalance(transaction, client);
        var currentBalance = client?.Balance ?? decimal.Zero;

        if (transaction.Debit.HasValue)
        {
            currentBalance += transaction.Debit.Value;
        }
        else if (transaction.Credit.HasValue)
        {
            currentBalance -= transaction.Credit.Value;
        }
        else
        {
            throw new ArgumentException(
                $"Transaction amount is not specified. Transaction ID: {transaction.TransactionId}");
        }

        var model = _mapper.Map<TransactionModel>(transaction);

        model.ClientBalance = currentBalance;
        if (client == null)
        {
            client = new ClientModel { Balance = currentBalance, ClientId = model.ClientId };
            await _repository.AddClientAsync(client);
        }
        else
        {
            client.Balance = currentBalance;
            _repository.UpdateClient(client);
        }

        await _repository.AddTransactionAsync(model);
        await _repository.SaveChangesAsync();

        return new TimeAndMoneyDto(model.CreatedDateUtc, model.ClientBalance);
    }

    public async Task<TimeAndMoneyDto> RevertTransactionAsync(Guid transactionId, Guid clientId)
    {
        var revertedTransaction = await _repository.GetTransactionByIdAsync(transactionId, withTracking: true);
        if (revertedTransaction == null)
        {
            throw new KeyNotFoundException($"Transaction not found. Id: {transactionId}");
        }

        if (revertedTransaction.RevertedById.HasValue)
        {
            var revertedBy =
                await _repository.GetTransactionByIdAsync(revertedTransaction.RevertedById.Value);
            return new TimeAndMoneyDto(revertedBy!.CreatedDateUtc, revertedBy.ClientBalance);
        }

        var compensatingTransaction = new TransactionModel
        {
            TransactionId = Guid.NewGuid(),
            ClientId = clientId,
            Date = DateTime.UtcNow,
        };

        var client = await _repository.GetClientByIdAsync(clientId);
        var currentBalance = client!.Balance;

        if (revertedTransaction.Credit.HasValue)
        {
            compensatingTransaction.ClientBalance = currentBalance + revertedTransaction.Credit.Value;
            compensatingTransaction.Credit = -revertedTransaction.Credit.Value;
        }
        else if (revertedTransaction.Debit.HasValue)
        {
            compensatingTransaction.ClientBalance = currentBalance - revertedTransaction.Debit.Value;
            compensatingTransaction.Debit = -revertedTransaction.Debit.Value;
        }
        client.Balance = compensatingTransaction.ClientBalance;
        _repository.UpdateClient(client);

        revertedTransaction.RevertedById = compensatingTransaction.TransactionId;
        await _repository.AddTransactionAsync(compensatingTransaction);
        _repository.UpdateTransaction(revertedTransaction);
        await _repository.SaveChangesAsync();

        return new TimeAndMoneyDto(compensatingTransaction.CreatedDateUtc, compensatingTransaction.ClientBalance);
    }

    private void ValidateBalance(TransactionDto transaction, ClientModel? client)
    {
        var currentBalance = client?.Balance ?? decimal.Zero;
        if (transaction.Credit.HasValue && transaction.Credit.Value > currentBalance)
        {
            throw new InsufficientAmountException($"Client {transaction.ClientId} has insufficient funds.");
        }
    }

    private void ValidateTime(DateTime transactionDateTime, ClientModel? client)
    {
        if (client != null && client.LastUpdated >= transactionDateTime)
        {
            throw new NotLastTransactionException(
                "There are more recent transactions for this client. " +
                $"DateTime specified {transactionDateTime}. " +
                $"Last transaction is on {client.LastUpdated}. " +
                $"Client: {client.ClientId}.");
        }
    }
}