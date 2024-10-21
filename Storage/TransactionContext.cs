using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TransactionManager.Storage.Models;

namespace TransactionManager.Storage;

public class TransactionContext : DbContext
{
    public DbSet<TransactionModel> Transactions { get; set; }

    public TransactionContext(DbContextOptions<TransactionContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine);
        //optionsBuilder.UseSqlite("Data Source=transactions.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Convert decimal to double since SQLite  does not support Decimal
        var decimalToDoubleConverter = new ValueConverter<decimal, double>(
            v => (double)v,
            v => (decimal)v
        );

        modelBuilder.Entity<TransactionModel>()
            .Property(t => t.Debit)
            .HasConversion(decimalToDoubleConverter);

        modelBuilder.Entity<TransactionModel>()
            .Property(t => t.Credit)
            .HasConversion(decimalToDoubleConverter);

        modelBuilder.Entity<TransactionModel>()
            .Property(b => b.CreatedDateUtc)
            .HasDefaultValueSql("DATETIME()");
        base.OnModelCreating(modelBuilder);
    }
}