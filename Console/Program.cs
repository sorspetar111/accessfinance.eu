using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using Interfaces;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

bool useSqlServer = !string.IsNullOrEmpty(connectionString);

// hardcoded
useSqlServer = false;

if (useSqlServer)
{

    Console.WriteLine("Configuring application to use SQL Server.");
    builder.Services.AddDbContext<Data.AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{

    Console.WriteLine("Configuring application to use In-Memory Database.");
    builder.Services.AddDbContext<Data.AppDbContext>(options =>
        options.UseInMemoryDatabase("FallbackFinanceDb"));
}



builder.Services.AddScoped<IAccountService, AccountService>();

var host = builder.Build();
await RunConsoleApp(host.Services);

static async Task RunConsoleApp(IServiceProvider services)
{

    using var scope = services.CreateScope();
    var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

    var dbContext = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
    bool isMemory = dbContext.Database.IsInMemory();
    if (isMemory)
        Console.WriteLine("In-Memory database...");

    // TODO: do you want some dummy initi data?
    // await accountService.CreateAccountAsync("Alice", "111-222", 1500);
    // await accountService.CreateAccountAsync("Bob", "333-444", 800);

    while (true)
    {
        Console.WriteLine("\n--- Transaction System Menu ---");
        Console.WriteLine("1. Create a new Account");
        Console.WriteLine("2. Deposit Money");
        Console.WriteLine("3. Withdraw Money");
        Console.WriteLine("4. Check Account Balance");
        Console.WriteLine("5. Transfer Money");
        Console.WriteLine("6. Exit");
        Console.Write("Enter your choice: ");

        if (!int.TryParse(Console.ReadLine(), out var choice))
        {
            Console.WriteLine("Invalid choice. Please enter a number.");
            continue;
        }

        switch (choice)
        {
            case 1: await CreateAccount(accountService); break;
            case 2: await DepositMoney(accountService); break;
            case 3: await WithdrawMoney(accountService); break;
            case 4: await CheckAccountBalance(accountService); break;
            case 5: await TransferMoney(accountService); break;
            case 6: return;
            default: Console.WriteLine("Invalid choice. Please try again."); break;
        }
    }
}



static async Task CreateAccount(IAccountService service)
{
    Console.Write("Enter user name: ");
    var name = Console.ReadLine() ?? "";
    Console.Write("Enter a unique account number: ");
    var accountNumber = Console.ReadLine() ?? "";
    Console.Write("Enter initial balance: ");
    if (decimal.TryParse(Console.ReadLine(), out var balance))
    {
        var (Success, Message, _) = await service.CreateAccountAsync(name, accountNumber, balance);
        Console.WriteLine(Message);
    }
    else
    {
        Console.WriteLine("Invalid balance amount.");
    }
}

static async Task DepositMoney(IAccountService service)
{
    Console.Write("Enter account number: ");
    var accountNumber = Console.ReadLine() ?? "";
    Console.Write("Enter amount to deposit: ");
    if (decimal.TryParse(Console.ReadLine(), out var amount))
    {
        var (Success, Message) = await service.DepositAsync(accountNumber, amount);
        Console.WriteLine(Message);
    }
    else
    {
        Console.WriteLine("Invalid amount.");
    }
}

static async Task WithdrawMoney(IAccountService service)
{
    Console.Write("Enter account number: ");
    var accountNumber = Console.ReadLine() ?? "";
    Console.Write("Enter amount to withdraw: ");
    if (decimal.TryParse(Console.ReadLine(), out var amount))
    {
        var (Success, Message) = await service.WithdrawAsync(accountNumber, amount);
        Console.WriteLine(Message);
    }
    else
    {
        Console.WriteLine("Invalid amount.");
    }
}

static async Task CheckAccountBalance(IAccountService service)
{
    Console.Write("Enter account number: ");
    var accountNumber = Console.ReadLine() ?? "";
    var (Success, Message, Balance) = await service.GetAccountBalanceAsync(accountNumber);
    if (Success)
    {
        Console.WriteLine($"Account Balance: {Balance:C}");
    }
    else
    {
        Console.WriteLine(Message);
    }
}

static async Task TransferMoney(IAccountService service)
{
    Console.Write("Enter your account number (from): ");
    var fromAccount = Console.ReadLine() ?? "";
    Console.Write("Enter recipient's account number (to): ");
    var toAccount = Console.ReadLine() ?? "";
    Console.Write("Enter amount to transfer: ");
    if (decimal.TryParse(Console.ReadLine(), out var amount))
    {
        var (Success, Message) = await service.TransferAsync(fromAccount, toAccount, amount);
        Console.WriteLine(Message);
    }
    else
    {
        Console.WriteLine("Invalid amount.");
    }
}