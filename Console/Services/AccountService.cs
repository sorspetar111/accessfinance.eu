/*


the in-memory database provider does not have any concept of transaction isolation levels like READ COMMITTED for real relation database variant: 
- Pessimistic Concurrency

 await using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  ... SaveChangesAsync()
  await dbTransaction.CommitAsync(); 
  ... return (true, "Withdrawal successful.");



- Optimistic Concurrency
    // Add this property in Account model
    public byte[] RowVersion { get; set; }

    // in context for concurrency token 
    modelBuilder.Entity<Account>(entity =>
    {    
        ...
        entity.Property(a => a.RowVersion).IsRowVersion(); 
    });


    try 
    {
    

    }     
    catch (DbUpdateConcurrencyException)
    {        
        return (false, "The transaction could not be completed because the account was modified by another user. Please try again.");
    }
    catch (Exception)
    {        
        return (false, "An unexpected error occurred.");
    }

 - Hyper cache alternative (the best solution)
 GetOrAdd in-memory have this inmplementation alredy. the atomic! thread-safe!

  // for example:
    public Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        if (_accounts.TryGetValue(accountNumber, out var account))
        {
            return Task.FromResult((true, "Balance retrieved.", (decimal?)account.Balance));
        }
        return Task.FromResult((false, "Account not found.", (decimal?)null));
    }

*/


using Microsoft.EntityFrameworkCore;
using TransactionSystem.Data;
using TransactionSystem.Models;

namespace TransactionSystem.Services;

// TODO: To implement  the atomic! thread-safe! simulare to .net 9 hyper cache and GetOrAdd in-memory
public class AccountService : IAccountService
{
    private readonly AppDbContext _context;

    public AccountService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Account? Account)> CreateAccountAsync(string userName, string accountNumber, decimal initialBalance)
    {
        if (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber))
        {
            return (false, "An account with this number already exists.", null);
        }

        var user = new User { Name = userName, CreatedDate = DateTime.UtcNow };
        var account = new Account
        {
            User = user,
            AccountNumber = accountNumber,
            Balance = initialBalance,
            CreatedDate = DateTime.UtcNow
        };

        if (initialBalance > 0)
        {
            var initialDeposit = new Transaction
            {
                Account = account,
                Amount = initialBalance,
                Type = TransactionType.Deposit,
                Timestamp = DateTime.UtcNow,
                Description = "Initial deposit"
            };
            _context.Transactions.Add(initialDeposit);
        }

        _context.Users.Add(user);
        _context.Accounts.Add(account);
        
        await _context.SaveChangesAsync();
        return (true, "Account created successfully.", account);
    }

    public async Task<(bool Success, string Message)> DepositAsync(string accountNumber, decimal amount)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null) return (false, "Account not found.");
        if (amount <= 0) return (false, "Deposit amount must be positive.");

        account.Balance += amount;
        
        var transaction = new Transaction
        {
            AccountId = account.Id,
            Amount = amount,
            Type = TransactionType.Deposit,
            Timestamp = DateTime.UtcNow,
            Description = $"Deposit of {amount:C}"
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();
        return (true, "Deposit successful.");
    }

    public async Task<(bool Success, string Message)> WithdrawAsync(string accountNumber, decimal amount)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null) return (false, "Account not found.");
        if (amount <= 0) return (false, "Withdrawal amount must be positive.");
        if (account.Balance < amount) return (false, "Insufficient funds.");

        account.Balance -= amount;

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Amount = amount, 
            Type = TransactionType.Withdrawal,
            Timestamp = DateTime.UtcNow,
            Description = $"Withdrawal of {amount:C}"
        };
        _context.Transactions.Add(transaction);
        
        await _context.SaveChangesAsync();
        return (true, "Withdrawal successful.");
    }

    public async Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        var account = await _context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            
        if (account == null) return (false, "Account not found.", null);
        
        return (true, "Balance retrieved.", account.Balance);
    }

    public async Task<(bool Success, string Message)> TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (fromAccountNumber == toAccountNumber) return (false, "Source and destination accounts cannot be the same.");
        if (amount <= 0) return (false, "Transfer amount must be positive.");

      
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fromAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == fromAccountNumber);
            if (fromAccount == null) return (false, "Source account not found.");
            if (fromAccount.Balance < amount) return (false, "Insufficient funds in source account.");

            var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber);
            if (toAccount == null) return (false, "Destination account not found.");

          
            fromAccount.Balance -= amount;
            var withdrawalTx = new Transaction
            {
                AccountId = fromAccount.Id,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Timestamp = DateTime.UtcNow,
                Description = $"Transfer to {toAccountNumber}"
            };
            _context.Transactions.Add(withdrawalTx);

         
            toAccount.Balance += amount;
            var depositTx = new Transaction
            {
                AccountId = toAccount.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Timestamp = DateTime.UtcNow,
                Description = $"Transfer from {fromAccountNumber}"
            };
            _context.Transactions.Add(depositTx);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Transfer successful.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
           
            return (false, "An error occurred during the transfer.");
        }
    }
}