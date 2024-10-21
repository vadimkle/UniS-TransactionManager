using System.Text.Json.Serialization;

namespace TransactionManager.Views;

public class InsertTransactionResult
{
    [JsonPropertyName("insertDateTime")] public DateTime InsertDateTime { get; set; }
    [JsonPropertyName("clientBalance")] public decimal ClientBalance { get; set; }
}
public class RevertTransactionResult
{
    [JsonPropertyName("revertDateTime")] public DateTime RevertDateTime { get; set; }
    [JsonPropertyName("clientBalance")] public decimal ClientBalance { get; set; }
}
public class ClientBalanceResult
{
    [JsonPropertyName("balanceDateTime")] public DateTime? BalanceDateTime { get; set; }
    [JsonPropertyName("clientBalance")] public decimal ClientBalance { get; set; }
}