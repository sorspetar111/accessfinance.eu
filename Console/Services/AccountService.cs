
using Data;
using Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using System.Data;

namespace Services;

/*
Example with highest, strong  and slowwest transaction level protection - Serializable
*/

public class AccountService : IAccountService
{
    private readonly Data.AppDbContext _context;

    public AccountService(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Models.Account? Account)> CreateAccountAsync(
    string userName, string accountNumber, decimal initialBalance)
    {
        var isRelational = _context.Database.IsRelational();
        IDbContextTransaction? dbTransaction = null;

        if (isRelational)
            dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            if (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber))
            {
                return (false, "An account with this number already exists.", null);
            }

            var user = new Models.User { Name = userName };
            var account = new Models.Account
            {
                User = user,
                AccountNumber = accountNumber,
                Balance = initialBalance
            };

            if (initialBalance > 0)
            {
                _context.Transactions.Add(new Models.Transaction
                {
                    Account = account,
                    Amount = initialBalance,
                    Type = Enums.TransactionType.Deposit,
                    Description = "Initial deposit"
                });
            }

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            if (isRelational)
                await dbTransaction!.CommitAsync();

            return (true, "Account created successfully.", account);
        }
        catch (Exception ex)
        {
            if (isRelational && dbTransaction != null)
                await dbTransaction.RollbackAsync();

            return (false, $"An unexpected database error occurred: {ex.Message}", null);
        }
    }



    public async Task<(bool Success, string Message)> DepositAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return (false, "Deposit amount must be positive.");

        var isRelational = _context.Database.IsRelational();
        IDbContextTransaction? dbTransaction = null;

        if (isRelational)
            dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // For relational: use locking hints
            // For in-memory: normal LINQ query
            var accountQuery = isRelational
                ? _context.Accounts.FromSqlRaw(
                    "SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}",
                    accountNumber)
                : _context.Accounts.Where(a => a.AccountNumber == accountNumber);

            var account = await accountQuery.FirstOrDefaultAsync();

            if (account == null)
                return (false, "Account not found.");

            account.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = $"Deposit of {amount:C}"
            });

            await _context.SaveChangesAsync();

            if (isRelational)
                await dbTransaction!.CommitAsync();

            return (true, "Deposit successful.");
        }
        catch (Exception ex)
        {
            if (isRelational && dbTransaction != null)
                await dbTransaction.RollbackAsync();

            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> WithdrawAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return (false, "Withdrawal amount must be positive.");

        var isRelational = _context.Database.IsRelational();
        IDbContextTransaction? dbTransaction = null;

        if (isRelational)
            dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var accountQuery = isRelational
                ? _context.Accounts.FromSqlRaw(
                    "SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}",
                    accountNumber)
                : _context.Accounts.Where(a => a.AccountNumber == accountNumber);

            var account = await accountQuery.FirstOrDefaultAsync();

            if (account == null)
                return (false, "Account not found.");

            if (account.Balance < amount)
                return (false, "Insufficient funds.");

            account.Balance -= amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Description = $"Withdrawal of {amount:C}"
            });

            await _context.SaveChangesAsync();

            if (isRelational)
                await dbTransaction!.CommitAsync();

            return (true, "Withdrawal successful.");
        }
        catch (Exception ex)
        {
            if (isRelational && dbTransaction != null)
                await dbTransaction.RollbackAsync();

            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (fromAccountNumber == toAccountNumber)
            return (false, "Source and destination accounts cannot be the same.");

        if (amount <= 0)
            return (false, "Transfer amount must be positive.");

        var isRelational = _context.Database.IsRelational();
        IDbContextTransaction? dbTransaction = null;

        if (isRelational)
            dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var fromQuery = isRelational
                ? _context.Accounts.FromSqlRaw(
                    "SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}",
                    fromAccountNumber)
                : _context.Accounts.Where(a => a.AccountNumber == fromAccountNumber);

            var toQuery = isRelational
                ? _context.Accounts.FromSqlRaw(
                    "SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}",
                    toAccountNumber)
                : _context.Accounts.Where(a => a.AccountNumber == toAccountNumber);

            var fromAccount = await fromQuery.FirstOrDefaultAsync();
            var toAccount = await toQuery.FirstOrDefaultAsync();

            if (fromAccount == null)
                return (false, "Source account not found.");

            if (toAccount == null)
                return (false, "Destination account not found.");

            if (fromAccount.Balance < amount)
                return (false, "Insufficient funds in source account.");

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = fromAccount.Id,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Description = $"Transfer to {toAccountNumber}"
            });

            _context.Transactions.Add(new Transaction
            {
                AccountId = toAccount.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = $"Transfer from {fromAccountNumber}"
            });

            await _context.SaveChangesAsync();

            if (isRelational)
                await dbTransaction!.CommitAsync();

            return (true, "Transfer successful.");
        }
        catch (Exception ex)
        {
            if (isRelational && dbTransaction != null)
                await dbTransaction.RollbackAsync();

            return (false, $"An error occurred during the transfer: {ex.Message}");
        }
    }


    /*
    public async Task<(bool Success, string Message, Models.Account? Account)> CreateAccountAsync_(string userName, string accountNumber, decimal initialBalance)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber))
            {
                return (false, "An account with this number already exists.", null);
            }

            var user = new Models.User { Name = userName };
            var account = new Models.Account
            {
                User = user,
                AccountNumber = accountNumber,
                Balance = initialBalance
            };

            if (initialBalance > 0)
            {
                _context.Transactions.Add(new Models.Transaction
                {
                    Account = account,
                    Amount = initialBalance,
                    Type = Enums.TransactionType.Deposit,
                    Description = "Initial deposit"
                });
            }

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Account created successfully.", account);
        }
        catch (Exception ex)
        {

            return (false, $"An unexpected database error occurred: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DepositAsync_(string accountNumber, decimal amount)
    {
        if (amount <= 0) return (false, "Deposit amount must be positive.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {

            var account = await _context.Accounts
                .FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}", accountNumber)
                .FirstOrDefaultAsync();

            if (account == null) return (false, "Account not found.");

            account.Balance += amount;

            _context.Transactions.Add(new Models.Transaction
            {
                AccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = $"Deposit of {amount:C}"
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Deposit successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> WithdrawAsync_(string accountNumber, decimal amount)
    {
        if (amount <= 0) return (false, "Withdrawal amount must be positive.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {

            var account = await _context.Accounts
                .FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}", accountNumber)
                .FirstOrDefaultAsync();

            if (account == null) return (false, "Account not found.");
            if (account.Balance < amount) return (false, "Insufficient funds.");

            account.Balance -= amount;

            _context.Transactions.Add(new Models.Transaction
            {
                AccountId = account.Id,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Description = $"Withdrawal of {amount:C}"
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Withdrawal successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> TransferAsync_(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (fromAccountNumber == toAccountNumber) return (false, "Source and destination accounts cannot be the same.");
        if (amount <= 0) return (false, "Transfer amount must be positive.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {

            var fromAccount = await _context.Accounts
                .FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}", fromAccountNumber)
                .FirstOrDefaultAsync();

            var toAccount = await _context.Accounts
                .FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}", toAccountNumber)
                .FirstOrDefaultAsync();

            if (fromAccount == null) return (false, "Source account not found.");
            if (toAccount == null) return (false, "Destination account not found.");
            if (fromAccount.Balance < amount) return (false, "Insufficient funds in source account.");


            fromAccount.Balance -= amount;
            toAccount.Balance += amount;


            _context.Transactions.Add(new Models.Transaction { AccountId = fromAccount.Id, Amount = amount, Type = TransactionType.Withdrawal, Description = $"Transfer to {toAccountNumber}" });
            _context.Transactions.Add(new Models.Transaction { AccountId = toAccount.Id, Amount = amount, Type = TransactionType.Deposit, Description = $"Transfer from {fromAccountNumber}" });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Transfer successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred during the transfer: {ex.Message}");
        }
    }
    */

    public async Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        var account = await _context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null) return (false, "Account not found.", null);

        return (true, "Balance retrieved.", account.Balance);
    }


}