namespace Models;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}