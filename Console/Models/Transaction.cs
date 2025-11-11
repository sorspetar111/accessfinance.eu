using Enums;

namespace Models;

public class Transaction
{
    public Account Account { get; set; } = null!;
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionType Type { get; set; }
}