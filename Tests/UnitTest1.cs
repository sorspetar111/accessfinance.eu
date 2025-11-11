using Data;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Services;

namespace Tests
{
    [TestFixture]
    public class AccountServiceTests
    {
        private AppDbContext _context = null!;
        private IAccountService _service = null!;

        [SetUp]
        public async Task Setup()
        {
            // Create a unique in-memory DB for each test
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
          
            _context = new AppDbContext(options);
           
            var factory = new FactoryAccountService(_context);
            _service = factory.Create();
           
            // _service = new AccountService(_context);

            // Seed data
            await _service.CreateAccountAsync("Alice", "123", 1000);
            await _service.CreateAccountAsync("Bob", "456", 500);
        }

        [TearDown]
        public void TearDown()
        {
            // Properly dispose EF context after each test
            _context.Dispose();
        }

        [Test]
        public async Task CreateAccount_Success()
        {
            var result = await _service.CreateAccountAsync("Charlie", "789", 200);
            Assert.IsTrue(result.Success);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == "789");
            Assert.IsNotNull(account);
            Assert.AreEqual(200, account!.Balance);
        }

        [Test]
        public async Task CreateAccount_DuplicateFails()
        {
            var result = await _service.CreateAccountAsync("David", "123", 300);
            Assert.IsFalse(result.Success);
        }

        [Test]
        public async Task Deposit_Success()
        {
            var result = await _service.DepositAsync("123", 200);
            Assert.IsTrue(result.Success);
            var account = await _context.Accounts.FirstAsync(a => a.AccountNumber == "123");
            Assert.AreEqual(1200, account.Balance);
        }

        [Test]
        public async Task Withdraw_Success()
        {
            var result = await _service.WithdrawAsync("123", 200);
            Assert.IsTrue(result.Success);
            var account = await _context.Accounts.FirstAsync(a => a.AccountNumber == "123");
            Assert.AreEqual(800, account.Balance);
        }

        [Test]
        public async Task Withdraw_InsufficientFunds_Fails()
        {
            var result = await _service.WithdrawAsync("456", 600);
            Assert.IsFalse(result.Success);
            var account = await _context.Accounts.FirstAsync(a => a.AccountNumber == "456");
            Assert.AreEqual(500, account.Balance);
        }

        [Test]
        public async Task Transfer_Success()
        {
            var result = await _service.TransferAsync("123", "456", 300);
            Assert.IsTrue(result.Success);

            var from = await _context.Accounts.FirstAsync(a => a.AccountNumber == "123");
            var to = await _context.Accounts.FirstAsync(a => a.AccountNumber == "456");

            Assert.AreEqual(700, from.Balance);
            Assert.AreEqual(800, to.Balance);
        }

        [Test]
        public async Task Transfer_InsufficientFunds_Fails()
        {
            var result = await _service.TransferAsync("456", "123", 600);
            Assert.IsFalse(result.Success);

            var from = await _context.Accounts.FirstAsync(a => a.AccountNumber == "456");
            var to = await _context.Accounts.FirstAsync(a => a.AccountNumber == "123");

            Assert.AreEqual(500, from.Balance);
            Assert.AreEqual(1000, to.Balance);
        }
    }
}
