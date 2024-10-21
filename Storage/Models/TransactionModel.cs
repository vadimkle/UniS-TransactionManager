using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionManager.Storage.Models;

[Index(nameof(TransactionId), IsUnique = true)]
[Index(nameof(ClientId))]
public class TransactionModel
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key, Column(Order = 0)]
    public int Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid ClientId { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public decimal ClientBalance { get; set; }
    public DateTime Date { get; set; }
    public Guid? RevertedById { get; set; }
    public DateTime CreatedDateUtc { get; set; }
}