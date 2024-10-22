namespace TransactionManager.Dtos;

public class TimeAndMoneyDto
{
    public TimeAndMoneyDto(DateTime dateTime, decimal balance)
    {
        BalanceDateTime = dateTime;
        Balance = balance;
    }

    public TimeAndMoneyDto()
    {
    }

    public DateTime BalanceDateTime { get; set; }
    public decimal Balance { get; set; }
}