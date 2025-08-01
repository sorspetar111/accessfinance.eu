using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TransactionSystem.Models;
/*

public class Account
{
    public string AccountNumber { get; }
    public string OwnerName { get; }
    public decimal Balance { get; private set; }
    private readonly object _balanceLock = new object();

    public Account(string accountNumber, string ownerName, decimal initialBalance)
    {
        AccountNumber = accountNumber;
        OwnerName = ownerName;
        Balance = initialBalance;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        }

        lock (_balanceLock)
        {
            Balance += amount;
        }
    }

    public bool Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        }

        lock (_balanceLock)
        {
            if (Balance < amount)
            {
                return false;
            }
            Balance -= amount;
            return true;
        }
    }
}

*/


public class Account
{
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedDate { get; set; }

  
    public Guid UserId { get; set; }
 
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
