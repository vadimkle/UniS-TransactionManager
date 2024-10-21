namespace TransactionManager.Exceptions;

public class InsufficientAmountException : InvalidOperationException
{
    public InsufficientAmountException(string message) : base(message)
    {
    }
}