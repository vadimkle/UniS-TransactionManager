namespace TransactionManager.Dtos;

public class TransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime DateTime { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
}

public class ReversionDto
{
    private Guid TargetTransactionId { get; set; }
}