using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Services;
using Interfaces;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

bool useSqlServer = !string.IsNullOrEmpty(connectionString);

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

// builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<FactoryAccountService>();
builder.Services.AddScoped<IAccountService>(sp =>
{
    var factory = sp.GetRequiredService<FactoryAccountService>();
    return factory.Create();
});

var host = builder.Build();

await ShellOperation.RunConsoleApp(host.Services);
