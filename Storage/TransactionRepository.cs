using Microsoft.EntityFrameworkCore;
using System.Data;
using TransactionManager.Storage.Models;

namespace TransactionManager.Storage
{
    public class TransactionRepository
    {
        private readonly TransactionContext _context;

        public TransactionRepository(TransactionContext context)
        {
            _context = context;
        }

        public async Task<TransactionModel?> GetTransactionByIdAsync(Guid transactionId, bool withTracking = false)
        {
            if (withTracking)
            {
                return await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            }
            return await _context.Transactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<(DateTime Date, decimal ClientBalance)> AddTransactionAsync(TransactionModel transaction)
        {
            await using (var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    var lastTransaction = await _context.Transactions
                        .OrderByDescending(t => t.CreatedDateUtc)
                        .FirstOrDefaultAsync(t => t.ClientId == transaction.ClientId);

                    if (lastTransaction != null && lastTransaction.Date >= transaction.Date)
                    {
                        throw new InvalidOperationException(
                            "There are more recent transactions for this client. " +
                            $"DateTime specified {transaction.Date}. " +
                            $"Client: {transaction.ClientId}.");
                    }

                    var currentBalance = lastTransaction?.ClientBalance?? decimal.Zero;
                    if (transaction.Credit.HasValue && transaction.Credit.Value > currentBalance)
                    {
                        throw new InvalidOperationException("Insufficient funds.");
                    }

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
                        throw new ArgumentException("Transaction amount is not specified");
                    }

                    transaction.ClientBalance = currentBalance;

                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            return (transaction.CreatedDateUtc, transaction.ClientBalance);
        }

        public async Task<decimal> GetClientBalanceAsync(Guid clientId)
        {
            var transaction = await _context.Transactions
                .OrderByDescending(t => t.CreatedDateUtc)
                .FirstOrDefaultAsync(t => t.ClientId == clientId);

            if (transaction == null)
            {
                throw new KeyNotFoundException($"Client not found. Client ID: {clientId}.");
            }

            return transaction?.ClientBalance ?? decimal.Zero;
        }

        internal async Task<TransactionModel?> GetLastClientTransactionAsync(TransactionModel model)
        {
            return await _context.Transactions.AsNoTracking()
                .OrderByDescending(t => t.CreatedDateUtc)
                .FirstOrDefaultAsync(t => t.ClientId == model.ClientId);
        }
    }
}
