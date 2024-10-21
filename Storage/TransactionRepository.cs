using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public IQueryable<TransactionModel> ListAll()
        {
            return _context.Transactions.AsNoTracking();
        }

        public async Task<TransactionModel?> GetByIdAsync(Guid transactionId, bool withTracking = false)
        {
            if (withTracking)
            {
                return await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            }
            return await _context.Transactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<EntityEntry<TransactionModel>> AddAsync(TransactionModel model)
        {
            return await _context.Transactions.AddAsync(model);
        }

        public EntityEntry<TransactionModel> Update(TransactionModel model)
        {
            return _context.Transactions.Update(model);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
