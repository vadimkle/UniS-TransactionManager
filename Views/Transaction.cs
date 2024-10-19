using System.ComponentModel.DataAnnotations;
using TransactionManager.Validators;

namespace TransactionManager.Views;

public interface ITransaction
{
    Guid Id { get; }
    Guid ClientId { get; }
    DateTime DateTime { get; }
    decimal Amount { get; }
}

public class CreditTransaction : ITransaction
{
    [Required]
    public Guid Id { get; private set; }
    [Required]
    public Guid ClientId { get; private set; }
    [DateTimeLessThanOrEqualToNow] public DateTime DateTime { get; private set; }
    [Required]
    [Range(Double.Epsilon, Double.MaxValue, ErrorMessage = "Transaction amount must be positive")]
    public decimal Amount { get; private set; }

    public CreditTransaction(Guid id, Guid clientId, DateTime dateTime, decimal amount)
    {
        Id = id;
        ClientId = clientId;
        DateTime = dateTime;
        Amount = amount;
    }
}

public class DebitTransaction : ITransaction
{
    [Required]
    public Guid Id { get; private set; }
    [Required]
    public Guid ClientId { get; private set; }
    [DateTimeLessThanOrEqualToNow]
    public DateTime DateTime { get; private set; }
    [Required]
    [Range(Double.Epsilon, Double.MaxValue, ErrorMessage = "Transaction amount must be positive")]
    public decimal Amount { get; private set; }

    public DebitTransaction(Guid id, Guid clientId, DateTime dateTime, decimal amount)
    {
        Id = id;
        ClientId = clientId;
        DateTime = dateTime;
        Amount = amount;
    }
}