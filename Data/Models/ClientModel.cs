using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionManager.Data.Models;

[Index(nameof(ClientId), IsUnique = true)]
public class ClientModel
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key, Column(Order = 0)]
    public int Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; }
}