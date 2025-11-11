
namespace Models;

public class Account
{
    public required string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid Id { get; set; }  
    public User User { get; set; } = null!;
    public Guid UserId { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
