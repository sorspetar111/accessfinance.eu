
using Data;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using System.Data;

namespace Services;

/*
Example with highest, strong  and slowwest transaction level protection - Serializable
*/

public class AccountServiceContext : AccountServiceMemory, IAccountService
{
    public AccountServiceContext(AppDbContext context) : base(context)
    {
    }

    public new async Task<(bool Success, string Message, Account? Account)> CreateAccountAsync(string userName, string accountNumber, decimal initialBalance)
    {
        IDbContextTransaction dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var result = await base.CreateAccountAsync(userName, accountNumber, initialBalance);
            await dbTransaction!.CommitAsync();

            return (result.Success, result.Message, result.Account);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, $"An unexpected database error occurred: {ex.Message}", null);
        }
    }

    public new async Task<(bool Success, string Message)> DepositAsync(string accountNumber, decimal amount)
    {
        IDbContextTransaction dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {

            var result = await base.DepositAsync(accountNumber, amount);
            await dbTransaction!.CommitAsync();

            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {         
            await dbTransaction.RollbackAsync();
            return (false, $"An unexpected database error occurred: {ex.Message}");
        }
    }

    public new async Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber)
    {
        return await base.GetAccountBalanceAsync(accountNumber);
    }

    public new async Task<(bool Success, string Message)> TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount)
    {
        if (fromAccountNumber == toAccountNumber)
            return (false, "Source and destination accounts cannot be the same.");

        if (amount <= 0)
            return (false, "Transfer amount must be positive.");
       
        IDbContextTransaction dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var result = await base.TransferAsync(fromAccountNumber, toAccountNumber, amount);
            await dbTransaction!.CommitAsync();

            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {

            await dbTransaction.RollbackAsync();

            return (false, $"An error occurred during the transfer: {ex.Message}");
        }
    }

}
