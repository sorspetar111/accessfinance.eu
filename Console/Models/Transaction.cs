namespace TransactionSystem.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }

  
    public Guid AccountId { get; set; }
  
    public Account Account { get; set; } = null!;
}