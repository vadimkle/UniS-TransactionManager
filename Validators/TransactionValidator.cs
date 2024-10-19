using TransactionManager.Views;

namespace TransactionManager.Validators;

public class TransactionValidator
{
    public void ValidateTransaction(ITransaction transaction)
    {
        // Additional validation could be placed here, e.g.
        // if (transaction.Amount > 3000)
        // {
        //     throw new ValidationException("Transaction require additional approval.");
        // }
    }
}