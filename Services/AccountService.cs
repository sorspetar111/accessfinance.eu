
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
    private readonly AppDbContext _context;

    public AccountService(AppDbContext context)
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

            var user = new User { Name = userName };
            var account = new Account
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

    public async Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        var account = await _context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null) return (false, "Account not found.", null);

        return (true, "Balance retrieved.", account.Balance);
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
            // 1. Safe way to get data depending on MSSQL server
            /*
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
            */

            // 2. This is another way to get data in safe concurent order
            var accountsTuple = await GetSafeAccounts(fromAccountNumber, toAccountNumber);


            if (accountsTuple.FromAccount == null)
                return (false, "Source account not found.");

            if (accountsTuple.ToAccount == null)
                return (false, "Destination account not found.");

            if (accountsTuple.FromAccount.Balance < amount)
                return (false, "Insufficient funds in source account.");

            accountsTuple.FromAccount.Balance -= amount;
            accountsTuple.ToAccount.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountsTuple.FromAccount.Id,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Description = $"Transfer to {accountsTuple.ToAccount.AccountNumber}"
            });

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountsTuple.ToAccount.Id,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = $"Transfer from {accountsTuple.FromAccount.AccountNumber}"
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
    private async Task<(Account? FromAccount, Account? ToAccount)> GetSafeAccounts(string fromAccountNumber, string toAccountNumber)
    {
        var accountNumbers = new[] { fromAccountNumber, toAccountNumber };

        var accounts = await _context.Accounts.Where(a => a.AccountNumber == accountNumbers[0])
            .Union(_context.Accounts.Where(a => a.AccountNumber == accountNumbers[1]))
            .ToListAsync();

        var fromAccount = accounts.FirstOrDefault(a => a.AccountNumber == accountNumbers[0]);
        var toAccount = accounts.FirstOrDefault(a => a.AccountNumber == accountNumbers[1]);

        return (fromAccount, toAccount);
    }


}