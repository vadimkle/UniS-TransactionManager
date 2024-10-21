using TransactionManager.Views;

namespace TransactionManager.Validators;

public class TransactionValidator
{
    public void ValidateTransaction(ITransaction transaction)
    {
        // Additional validation may goes here. For example:

        // if (transaction.Amount > 3000)
        // {
        //     throw new ValidationException("Transaction require additional approval.");
        // }
    }
}