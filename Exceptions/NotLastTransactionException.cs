namespace TransactionManager.Exceptions;

public class NotLastTransactionException : InvalidOperationException
{
    public NotLastTransactionException(string? message) : base(message)
    {
    }
}