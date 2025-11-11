using System;

[Obsolete("Use Program.cs instead.")]
public class FactoryProgram // : Program
{
    private static readonly TransactSystem _transactionSystem = new TransactSystem(null);

    public static void Main(string[] args)
    {
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
                case 1:
                    CreateAccount();
                    break;
                case 2:
                    DepositMoney();
                    break;
                case 3:
                    WithdrawMoney();
                    break;
                case 4:
                    CheckAccountBalance();
                    break;
                case 5:
                    TransferMoney();
                    break;
                case 6:
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }

    private static void CreateAccount()
    {
        Console.Write("Enter your name: ");
        string name = Console.ReadLine();
        Console.Write("Enter a unique account number: ");
        string accountNumber = Console.ReadLine();
        Console.Write("Enter initial balance: ");
        if (decimal.TryParse(Console.ReadLine(), out var balance))
        {
            if (_transactionSystem.CreateAccount(accountNumber, name, balance))
            {
                Console.WriteLine("Account created successfully.");
            }
            else
            {
                Console.WriteLine("An account with this number already exists.");
            }
        }
        else
        {
            Console.WriteLine("Invalid balance amount.");
        }
    }

    private static void DepositMoney()
    {
        Console.Write("Enter account number: ");
        string accountNumber = Console.ReadLine();
        Console.Write("Enter amount to deposit: ");
        if (decimal.TryParse(Console.ReadLine(), out var amount))
        {
            //if (_transactionSystem.Deposit(accountNumber, amount))
            //{
            //    Console.WriteLine("Deposit successful.");
            //}
            //else
            //{
            //    Console.WriteLine("Account not found.");
            //}
        }
        else
        {
            Console.WriteLine("Invalid amount.");
        }
    }

    private static void WithdrawMoney()
    {
        Console.Write("Enter account number: ");
        string accountNumber = Console.ReadLine();
        Console.Write("Enter amount to withdraw: ");
        if (decimal.TryParse(Console.ReadLine(), out var amount))
        {
            //if (_transactionSystem.Withdraw(accountNumber, amount))
            //{
            //    Console.WriteLine("Withdrawal successful.");
            //}
            //else
            //{
            //     var account = _transactionSystem.GetAccount(accountNumber);
            //    if (account == null)
            //    {
            //        Console.WriteLine("Account not found.");
            //    }
            //    else
            //    {
            //        Console.WriteLine("Insufficient balance.");
            //    }
            //}
        }
        else
        {
            Console.WriteLine("Invalid amount.");
        }
    }

    private static void CheckAccountBalance()
    {
        Console.Write("Enter account number: ");
        string accountNumber = Console.ReadLine();
        var account = _transactionSystem.GetAccount(accountNumber);
        if (account != null)
        {
            Console.WriteLine($"Account Balance: {account.Balance:C}");
        }
        else
        {
            Console.WriteLine("Account not found.");
        }
    }

    private static void TransferMoney()
    {
        Console.Write("Enter your account number (from): ");
        string fromAccountNumber = Console.ReadLine();
        Console.Write("Enter the recipient's account number (to): ");
        string toAccountNumber = Console.ReadLine();
        Console.Write("Enter amount to transfer: ");
        if (decimal.TryParse(Console.ReadLine(), out var amount))
        {
            //if (_transactionSystem.Transfer(fromAccountNumber, toAccountNumber, amount))
            //{
            //    Console.WriteLine("Transfer successful.");
            //}
            //else
            //{
            //    Console.WriteLine("Transfer failed. Check account numbers and balance.");
            //}
        }
        else
        {
            Console.WriteLine("Invalid amount.");
        }
    }
}