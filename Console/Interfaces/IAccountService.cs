using TransactionSystem.Models;

namespace TransactionSystem.Services;

public interface IAccountService
{
    Task<(bool Success, string Message, Account? Account)> CreateAccountAsync(string userName, string accountNumber, decimal initialBalance);
    Task<(bool Success, string Message)> DepositAsync(string accountNumber, decimal amount);
    Task<(bool Success, string Message)> WithdrawAsync(string accountNumber, decimal amount);
    Task<(bool Success, string Message, decimal? Balance)> GetAccountBalanceAsync(string accountNumber);
    Task<(bool Success, string Message)> TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount);
}