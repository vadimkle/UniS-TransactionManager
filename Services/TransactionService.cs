using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TransactionManager.Dtos;
using TransactionManager.Exceptions;
using TransactionManager.Storage;
using TransactionManager.Storage.Models;

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
        var transaction = await _repository.ListAll()
            .OrderByDescending(t => t.CreatedDateUtc)
            .FirstOrDefaultAsync(t => t.ClientId == clientId);

        if (transaction == null)
        {
            throw new KeyNotFoundException($"Client not found. Client ID: {clientId}.");
        }

        return new ClientBalanceDto
        { BalanceDateTime = transaction.CreatedDateUtc, Balance = transaction.ClientBalance };
    }

    public async Task<(DateTime insertDateTime, decimal clientBalance)> AddTransactionAsync(TransactionDto transaction)
    {
        var existingTransaction = await _repository.GetByIdAsync(transaction.TransactionId);
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

        await _repository.AddAsync(model);
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
        var revertingTransaction = await _repository.GetByIdAsync(transactionId, withTracking: true);
        if (revertingTransaction == null)
        {
            throw new KeyNotFoundException($"Transaction not found. Id: {transactionId}");
        }

        if (revertingTransaction.RevertedById.HasValue)
        {
            var revertedBy =
                await _repository.GetByIdAsync(revertingTransaction.RevertedById.Value);
            return (new DateTime(revertedBy!.CreatedDateUtc.Ticks, DateTimeKind.Utc), revertedBy.ClientBalance);
        }

        var compensatingTransaction = new TransactionModel
        {
            TransactionId = Guid.NewGuid(),
            ClientId = clientId,
            Date = DateTime.UtcNow,
        };

        var currentBalance = (await GetClientBalanceAsync(clientId)).Balance;
        if (revertingTransaction.Credit.HasValue)
        {
            compensatingTransaction.ClientBalance = currentBalance + revertingTransaction.Credit.Value;
            compensatingTransaction.Credit = -revertingTransaction.Credit.Value;
        }
        else if (revertingTransaction.Debit.HasValue)
        {
            compensatingTransaction.ClientBalance = currentBalance - revertingTransaction.Debit.Value;
            compensatingTransaction.Debit = -revertingTransaction.Debit.Value;
        }
        revertingTransaction.RevertedById = compensatingTransaction.TransactionId;
        await _repository.AddAsync(compensatingTransaction);
        _repository.Update(revertingTransaction);
        await _repository.SaveChangesAsync();

        return (new DateTime(compensatingTransaction.CreatedDateUtc.Ticks, DateTimeKind.Utc),
            compensatingTransaction.ClientBalance);
    }
}