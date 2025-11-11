using Interfaces;
using Models;
using Services;
using System;
using System.Collections.Concurrent;
using System.Threading;




public class TransactSystem
{
    private readonly ConcurrentDictionary<string, Account> _accounts = new ConcurrentDictionary<string, Account>();
    private readonly IAccountService service;


    public TransactSystem(IAccountService service)
    {
        this.service = service;
    }

    public bool CreateAccount(string accountNumber, string ownerName, decimal initialBalance)
    {

        var user = new User { Name = ownerName, CreatedDate = DateTime.UtcNow };
        var newAccount = new Account
        {
            User = user,
            AccountNumber = accountNumber,
            Balance = initialBalance,
            CreatedDate = DateTime.UtcNow
        };


        // var newAccount = new Account(accountNumber, ownerName, initialBalance);
        return _accounts.TryAdd(accountNumber, newAccount);
    }

    public Account GetAccount(string accountNumber)
    {
        _accounts.TryGetValue(accountNumber, out var account);
        return account;
    }

    public async Task <bool> Deposit(string accountNumber, decimal amount)
    {
        if (_accounts.TryGetValue(accountNumber, out var account))
        {
            var (Success, Message) = await service.DepositAsync(accountNumber, amount);         
            return Success;
        }
        return false;
    }

    public async Task<bool> Withdraw(string accountNumber, decimal amount)
    {
        if (_accounts.TryGetValue(accountNumber, out var account))
        {
            var (Success, Message) = await service.WithdrawAsync(accountNumber, amount);
            return Success;             
        }
        return false;
    }

    public async Task<bool> Transfer(string fromAccountNumber, string toAccountNumber, decimal amount)
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

        // lock (lock1)
        {
            // Thread.Sleep(100); // Simulate some work to increase chance of race conditions without locks
            //lock (lock2)
            {
                if (fromAccount.Balance >= amount)
                {
                    var (Success, Message) = await service.WithdrawAsync(fromAccountNumber, amount);
                    var (Success2, Message2) = await service.DepositAsync(toAccountNumber, amount);
                    
                    // fromAccount.Withdraw(amount);
                    // toAccount.Deposit(amount);

                    return Success && Success2;
                }
                return false; // Insufficient funds
            }
        }
    }
}