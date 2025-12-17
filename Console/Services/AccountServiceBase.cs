
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Data;

namespace Services;

public class AccountServiceBase
{
    protected readonly AppDbContext _context;

    public AccountServiceBase(AppDbContext context)
    {
        _context = context;
    }

    [Obsolete("Use safe")]
    protected async Task<Account> GetAccount(string accountNumber)
    {
        return _context.Database.IsRelational() ? await _context.Accounts.FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = {0}", accountNumber).FirstOrDefaultAsync() : await _context.Accounts.Where(a => a.AccountNumber == accountNumber).FirstOrDefaultAsync();
    }

    protected async Task<(Account? FromAccount, Account? ToAccount)> GetSafeAccounts(string fromAccountNumber, string toAccountNumber)
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

        var accountNumbers = new[] { fromAccountNumber, toAccountNumber };

        var accounts = await _context.Accounts.Where(a => a.AccountNumber == accountNumbers[0])
            .Union(_context.Accounts.Where(a => a.AccountNumber == accountNumbers[1]))
            .ToListAsync();

        var fromAccount = accounts.FirstOrDefault(a => a.AccountNumber == accountNumbers[0]);
        var toAccount = accounts.FirstOrDefault(a => a.AccountNumber == accountNumbers[1]);

        return (fromAccount, toAccount);
    }
}

