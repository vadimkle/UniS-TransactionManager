using AutoMapper;
using TransactionManager.Dtos;
using TransactionManager.Storage;
using TransactionManager.Storage.Models;

namespace TransactionManager.Services;

public class TransactionService
{
    private readonly TransactionRepository _repository;
    private readonly TransactionContext _context;
    private readonly IMapper _mapper;

    public TransactionService(TransactionRepository repository,
        TransactionContext context,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
        _context = context;
    }

    public async Task<decimal> GetClientBalanceAsync(Guid clientId)
    {
        return await _repository.GetClientBalanceAsync(clientId);
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
        await using (var dbTransaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var lastTransaction = await _repository.GetLastClientTransactionAsync(model);

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
                    throw new InvalidOperationException("Insufficient funds.");
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

                _context.Transactions.Add(model);
                // TODO here there is a risk of new record is added to the table and funds are insufficient
                // TODO use IsolationLevel.Serializable in such case
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        return (new DateTime(model.CreatedDateUtc.Ticks, DateTimeKind.Utc), model.ClientBalance);
    }

    public async Task<(DateTime revertDateTime, decimal clientBalance)> RevertTransactionAsync(Guid transactionId, Guid clientId)
    {
        var revertingTransaction = await _repository.GetTransactionByIdAsync(transactionId, withTracking: true);
        if (revertingTransaction == null)
        {
            throw new KeyNotFoundException($"Transaction not found. Id: {transactionId}");
        }

        if (revertingTransaction.RevertedById.HasValue)
        {
            var revertedBy =
                await _repository.GetTransactionByIdAsync(revertingTransaction.RevertedById.Value);
            return (new DateTime(revertedBy!.CreatedDateUtc.Ticks, DateTimeKind.Utc), revertedBy.ClientBalance);
        }

        var compensatingTransaction = new TransactionModel
        {
            TransactionId = Guid.NewGuid(),
            ClientId = clientId,
            Date = DateTime.UtcNow,
        };

        await using (var dbTransaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var currentBalance = await _repository.GetClientBalanceAsync(clientId);
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
                _context.Transactions.Add(compensatingTransaction);
                _context.Transactions.Update(revertingTransaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        return (new DateTime(compensatingTransaction.CreatedDateUtc.Ticks, DateTimeKind.Utc),
            compensatingTransaction.ClientBalance);
    }
}