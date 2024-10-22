using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TransactionManager.Data;
using TransactionManager.Data.Models;
using TransactionManager.Dtos;
using TransactionManager.Exceptions;

namespace TransactionManager.Services;

public class TransactionService
{
    private readonly TransactionRepository _repository;
    private readonly IMapper _mapper;

    public TransactionService(TransactionRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ClientBalanceDto> GetClientBalanceAsync(Guid clientId)
    {
        var client = await _repository.GetClientByIdAsync(clientId);

        if (client == null)
        {
            throw new KeyNotFoundException($"Client not found. Client ID: {clientId}.");
        }

        return new ClientBalanceDto
        { BalanceDateTime = client.LastUpdated, Balance = client.Balance };
    }

    public async Task<(DateTime insertDateTime, decimal clientBalance)> AddTransactionAsync(TransactionDto transaction)
    {
        var existingTransaction = await _repository.GetTransactionByIdAsync(transaction.TransactionId);
        if (existingTransaction != null)
        {
            return (new DateTime(existingTransaction.CreatedDateUtc.Ticks, DateTimeKind.Utc),
                existingTransaction.ClientBalance);
        }

        var model = _mapper.Map<TransactionModel>(transaction);
        var lastTransaction = await GetLastClientTransactionAsync(model.ClientId);

        if (lastTransaction != null && lastTransaction.Date >= model.Date)
        {
            throw new InvalidOperationException(
                "There are more recent transactions for this client. " +
                $"DateTime specified {model.Date}. " +
                $"Client: {model.ClientId}.");
        }

        var currentBalance = lastTransaction?.ClientBalance ?? decimal.Zero;
        if (model.Credit.HasValue && model.Credit.Value > currentBalance)
        {
            throw new InsufficientAmountException($"Client {model.ClientId} has insufficient funds.");
        }

        if (model.Debit.HasValue)
        {
            currentBalance += model.Debit.Value;
        }
        else if (model.Credit.HasValue)
        {
            currentBalance -= model.Credit.Value;
        }
        else
        {
            throw new ArgumentException(
                $"Transaction amount is not specified. Transaction ID: {model.TransactionId}");
        }

        model.ClientBalance = currentBalance;
        if (lastTransaction == null)
        {
            var client = new ClientModel { Balance = currentBalance, ClientId = model.ClientId };
            await _repository.AddClientAsync(client);
        }
        else
        {
            var client = await _repository.GetClientByIdAsync(model.ClientId);
            client!.Balance = currentBalance;
            _repository.UpdateClient(client);
        }

        await _repository.AddTransactionAsync(model);
        await _repository.SaveChangesAsync();

        return (new DateTime(model.CreatedDateUtc.Ticks, DateTimeKind.Utc), model.ClientBalance);
    }

    private async Task<TransactionModel?> GetLastClientTransactionAsync(Guid clientId)
    {
        return await _repository.ListAll()
            .OrderByDescending(t => t.CreatedDateUtc)
            .FirstOrDefaultAsync(t => t.ClientId == clientId);
    }

    public async Task<(DateTime revertDateTime, decimal clientBalance)> RevertTransactionAsync(Guid transactionId, Guid clientId)
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
            return (new DateTime(revertedBy!.CreatedDateUtc.Ticks, DateTimeKind.Utc), revertedBy.ClientBalance);
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

        return (new DateTime(compensatingTransaction.CreatedDateUtc.Ticks, DateTimeKind.Utc),
            compensatingTransaction.ClientBalance);
    }
}