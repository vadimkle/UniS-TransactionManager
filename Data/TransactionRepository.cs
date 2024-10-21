using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TransactionManager.Data.Models;

namespace TransactionManager.Data
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

        public async Task<ClientModel?> GetClientByIdAsync(Guid clientId)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == clientId);
        }

        public async Task<EntityEntry<TransactionModel>> AddTransactionAsync(TransactionModel model)
        {
            return await _context.Transactions.AddAsync(model);
        }

        public async Task<EntityEntry<ClientModel>> AddClientAsync(ClientModel model)
        {
            return await _context.Clients.AddAsync(model);
        }

        public EntityEntry<TransactionModel> UpdateTransaction(TransactionModel model)
        {
            return _context.Transactions.Update(model);
        }

        public EntityEntry<ClientModel> UpdateClient(ClientModel model)
        {
            return _context.Clients.Update(model);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
