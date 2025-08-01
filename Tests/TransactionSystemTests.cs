using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TransactionSystemTests
{
    private TransactionSystem _transactionSystem;

    [TestInitialize]
    public void Setup()
    {
        _transactionSystem = new TransactionSystem();
        _transactionSystem.CreateAccount("123", "Alice", 1000);
        _transactionSystem.CreateAccount("456", "Bob", 500);
    }

    [TestMethod]
    public void CreateAccount_Success()
    {
        bool result = _transactionSystem.CreateAccount("789", "Charlie", 200);
        Assert.IsTrue(result);
        Assert.IsNotNull(_transactionSystem.GetAccount("789"));
    }

    [TestMethod]
    public void CreateAccount_DuplicateFails()
    {
        bool result = _transactionSystem.CreateAccount("123", "David", 300);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Deposit_Success()
    {
        bool result = _transactionSystem.Deposit("123", 200);
        Assert.IsTrue(result);
        Assert.AreEqual(1200, _transactionSystem.GetAccount("123").Balance);
    }

    [TestMethod]
    public void Withdraw_Success()
    {
        bool result = _transactionSystem.Withdraw("123", 200);
        Assert.IsTrue(result);
        Assert.AreEqual(800, _transactionSystem.GetAccount("123").Balance);
    }

    [TestMethod]
    public void Withdraw_InsufficientFunds_Fails()
    {
        bool result = _transactionSystem.Withdraw("456", 600);
        Assert.IsFalse(result);
        Assert.AreEqual(500, _transactionSystem.GetAccount("456").Balance);
    }

    [TestMethod]
    public void Transfer_Success()
    {
        bool result = _transactionSystem.Transfer("123", "456", 300);
        Assert.IsTrue(result);
        Assert.AreEqual(700, _transactionSystem.GetAccount("123").Balance);
        Assert.AreEqual(800, _transactionSystem.GetAccount("456").Balance);
    }

    [TestMethod]
    public void Transfer_InsufficientFunds_Fails()
    {
        bool result = _transactionSystem.Transfer("456", "123", 600);
        Assert.IsFalse(result);
        Assert.AreEqual(500, _transactionSystem.GetAccount("456").Balance);
        Assert.AreEqual(1000, _transactionSystem.GetAccount("123").Balance);
    }
}