
using Data;
using Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services;

public class AccountServiceMemory : AccountServiceBase, IAccountService
{
    public AccountServiceMemory(AppDbContext context) : base(context)
    {
    }

    public async Task<(bool Success, string Message, Models.Account? Account)> CreateAccountAsync(string userName, string accountNumber, decimal initialBalance)
    {
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
            return (true, "Account created successfully.", account);
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected database error occurred: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DepositAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return (false, "Deposit amount must be positive.");

        try
        {
            var account = await GetAccount(accountNumber);

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


            return (true, "Deposit successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        var account = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null)
            return (false, "Account not found.", null);
        return (true, "Balance retrieved.", account.Balance);
    }

    public async Task<(bool Success, string Message)> TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (fromAccountNumber == toAccountNumber)
            return (false, "Source and destination accounts cannot be the same.");

        if (amount <= 0)
            return (false, "Transfer amount must be positive.");

        try
        {
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
            return (true, "Transfer successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred during the transfer: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> WithdrawAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return (false, "Withdrawal amount must be positive.");

        try
        {
            var account = await GetAccount(accountNumber);

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

            return (true, "Withdrawal successful.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

}
