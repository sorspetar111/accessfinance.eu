using System;
using System.Collections.Concurrent;
using System.Threading;


public class TransactionSystem
{
    private readonly ConcurrentDictionary<string, Account> _accounts = new ConcurrentDictionary<string, Account>();

    public bool CreateAccount(string accountNumber, string ownerName, decimal initialBalance)
    {
        var newAccount = new Account(accountNumber, ownerName, initialBalance);
        return _accounts.TryAdd(accountNumber, newAccount);
    }

    public Account GetAccount(string accountNumber)
    {
        _accounts.TryGetValue(accountNumber, out var account);
        return account;
    }

    public bool Deposit(string accountNumber, decimal amount)
    {
        if (_accounts.TryGetValue(accountNumber, out var account))
        {
            account.Deposit(amount);
            return true;
        }
        return false;
    }

    public bool Withdraw(string accountNumber, decimal amount)
    {
        if (_accounts.TryGetValue(accountNumber, out var account))
        {
            return account.Withdraw(amount);
        }
        return false;
    }

    public bool Transfer(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Transfer amount must be positive.");
        }

        if (!_accounts.TryGetValue(fromAccountNumber, out var fromAccount) || !_accounts.TryGetValue(toAccountNumber, out var toAccount))
        {
            return false; // One or both accounts not found
        }

        // To prevent deadlocks, always lock accounts in the same order.
        var lock1 = fromAccountNumber.CompareTo(toAccountNumber) < 0 ? fromAccount : toAccount;
        var lock2 = fromAccountNumber.CompareTo(toAccountNumber) < 0 ? toAccount : fromAccount;

        lock (lock1)
        {
            Thread.Sleep(100); // Simulate some work to increase chance of race conditions without locks
            lock (lock2)
            {
                if (fromAccount.Balance >= amount)
                {
                    fromAccount.Withdraw(amount);
                    toAccount.Deposit(amount);
                    return true;
                }
                return false; // Insufficient funds
            }
        }
    }
}