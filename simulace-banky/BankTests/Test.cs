using SimulaceBanky;
using SimulaceBanky.Users;

namespace BankTests
{
    [TestClass]
    public sealed class BankingTests : TestBase
    {
        // -----------------------------
        // 1) CREDIT INTEREST TESTS
        // -----------------------------

        [TestMethod]
        public void CreditInterest_ShouldBeZero_WhenWithinFreePeriod()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 28), -1000 },
                { new DateTime(2026, 3, 31), -1000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Credit, balances);

            Assert.AreEqual(0m, result);
        }

        [TestMethod]
        public void CreditInterest_ShouldApply_AfterFreePeriod()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), -1000 },
                { new DateTime(2026, 3, 20), -1000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Credit, balances);

            Assert.IsTrue(result < 0);
        }

        [TestMethod]
        public void CreditInterest_ShouldIgnorePositiveBalances()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), 1000 },
                { new DateTime(2026, 3, 20), -1000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Credit, balances);

            Assert.IsTrue(result < 0);
        }

        [TestMethod]
        public void CreditInterest_ShouldBeZero_WhenAllPositive()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), 500 },
                { new DateTime(2026, 3, 20), 1000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Credit, balances);

            Assert.AreEqual(0m, result);
        }

        // -----------------------------
        // 2) SAVINGS INTEREST TESTS
        // -----------------------------

        [TestMethod]
        public void SavingsInterest_ShouldBePositive_WhenBalanceIsPositive()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), 1000 },
                { new DateTime(2026, 3, 31), 1000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            Assert.IsTrue(result > 0);
        }

        [TestMethod]
        public void SavingsInterest_ShouldBeZero_WhenBalanceIsZero()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), 0 },
                { new DateTime(2026, 3, 31), 0 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            Assert.AreEqual(0m, result);
        }

        [TestMethod]
        public void SavingsInterest_ShouldHandleChangingBalances()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), 1000 },
                { new DateTime(2026, 3, 15), 2000 },
                { new DateTime(2026, 3, 31), 3000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            Assert.IsTrue(result > 0);
        }

        [TestMethod]
        public void SavingsInterest_ShouldBeNegative_WhenBalanceIsNegative()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), -500 },
                { new DateTime(2026, 3, 31), -500 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            Assert.IsTrue(result < 0);
        }

        [TestMethod]
        public void SavingsInterest_ShouldCalculateCorrectly_ForMixedPositiveAndNegative()
        {
            var balances = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 3, 1), -1000 },
                { new DateTime(2026, 3, 20), 2000 },
                { new DateTime(2026, 3, 31), 2000 }
            };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            Assert.AreNotEqual(0m, result);
        }
        [DataTestMethod]
        [DataRow(1000, 1000, 1000)]
        [DataRow(500, 1500, 2500)]
        [DataRow(2000, 2000, 5000)]
        public void SavingsInterest_ShouldMatchExactRoundedResult(
            int startBalanceInt,
            int midBalanceInt,
            int endBalanceInt
        )
        {

            decimal startBalance = startBalanceInt;
            decimal midBalance = midBalanceInt;
            decimal endBalance = endBalanceInt;
            decimal expectedAvgBalance = (startBalance * 14 + midBalance * 16 + endBalance * 1) / 31;

            var balances = new Dictionary<DateTime, decimal>
        {
            { new DateTime(2026, 3, 1), startBalance },
            { new DateTime(2026, 3, 15), midBalance },
            { new DateTime(2026, 3, 31), endBalance }
        };

            decimal result = Bank.CalculateMonthlyInterest(AccountType.Savings, balances);

            decimal rate = Bank.GetAnnualRate(AccountType.Savings);
            decimal expectedInterest = Bank.Round((expectedAvgBalance * rate) / 12m);

            Assert.AreEqual(expectedInterest, result);
        }


        // -----------------------------
        // 3) USER VERFIY & PASSWORD HASHING TESTS
        // -----------------------------
        [TestMethod]
        public void GetAndVerify_ShouldReturnUser_WhenCredentialsAreCorrect()
        {
            int id = CreateUser("Test", "User", Roles.Client, "testlogin", "pass");

            var user = User.GetAndVerify(conn, "testlogin", "pass");

            Assert.IsNotNull(user);
            Assert.AreEqual(id, user.Id);
            Assert.AreEqual("Test", user.Name);
            Assert.AreEqual(Roles.Client, user.Role);
        }

        [TestMethod]
        public void GetAndVerify_ShouldReturnNull_WhenPasswordIsWrong()
        {
            CreateUser("Test", "User", Roles.Client, "testlogin", "pass");

            var user = User.GetAndVerify(conn, "testlogin", "wrong");

            Assert.IsNull(user);
        }

        [TestMethod]
        public void GetAndVerify_ShouldReturnNull_WhenLoginDoesNotExist()
        {
            var user = User.GetAndVerify(conn, "nope", "pass");

            Assert.IsNull(user);
        }
        // -----------------------------
        // 4) ADMIN TESTS
        // -----------------------------
        [TestMethod]
        public void CreateUser_ShouldInsertUser_AndLogCreation()
        {
            var admin = new Admin(conn) { Id = 1 };

            admin.CreateUser("John", "Doe", Roles.Client, "johnddd", "secret");

            var users = User.GetUsers(conn);
            Assert.IsTrue(users.Any(u => u.Login == "johnddd"));

            var logs = Logger.GetActivity();
            Assert.IsTrue(logs.Any(l => l.Type == "CreateUser"));
        }
        // -----------------------------
        // 5) ACCOUNT TESTS
        // -----------------------------
        [TestMethod]
        public void OpenAccount_ShouldCreateAccount_AndLogCreation()
        {
            int userId = CreateUser("A", "B", Roles.Client, "x", "y");

            Account.OpenAccount(conn, AccountType.Savings, userId, bankerId: 1);

            var accounts = Account.GetAccounts(conn);
            Assert.IsTrue(accounts.Any(a => a._Owner.Id == userId));

            var logs = Logger.GetActivity();
            Assert.IsTrue(logs.Any(l => l.Type == "CreateAccount"));
        }
        // -----------------------------
        // 6) CHANGE BALANCE TESTS
        // -----------------------------
        [TestMethod]
        public void ChangeBalance_ShouldTransferMoney_WhenValid()
        {
            int bankerId = CreateUser("Banker", "Test", Roles.Banker, "bankerTest", "pass");
            var banker = new Banker(conn) { Id = bankerId };

            int user1 = CreateUser("A", "B", Roles.Client, "u1", "p");

            banker.OpenAccount(AccountType.Basic, user1);
            banker.OpenAccount(AccountType.Savings, user1);

            var acc1 = Account.GetAccounts(conn).First(a => a._Owner.Id == user1 && a.AccountType == AccountType.Basic);
            var acc2 = Account.GetAccounts(conn).First(a => a._Owner.Id == user1 && a.AccountType == AccountType.Savings);
            Console.WriteLine(acc1._Owner.Id);

            // deposit
            var a1 = (BasicAccount)Account.GetAccounts(conn).First(a => a.Id == acc1.Id);
            a1.Deposit(1000);

            var from = (BasicAccount)Account.GetAccounts(conn).First(a => a.Id == acc1.Id);
            var to = Account.GetAccounts(conn).First(a => a.Id == acc2.Id);

            bool ok = from.TransferFunds(to, 300);

            Assert.IsTrue(ok);
            Assert.AreEqual(700, GetBalance(acc1.Id));
            Assert.AreEqual(300, GetBalance(acc2.Id));
        }

        [TestMethod]
        public void ChangeBalance_ShouldFail_WhenExceedingMaxPaymentLimit()
        {
            int user1 = CreateUser("A", "B", Roles.Client, "u1", "p");
            int user2 = CreateUser("C", "D", Roles.Client, "u2", "p");
            int bankerId = CreateUser("Banker", "Test", Roles.Banker, "bankerTest", "pass");
            var banker = new Banker(conn) { Id = bankerId };

            banker.OpenAccount(AccountType.Basic, user1);
            banker.OpenAccount(AccountType.Savings, user2);

            // deposit to acc1
            var a1 = (BasicAccount)Account.GetAccounts(conn).First(a => a._Owner.Id == user1 && a.AccountType == AccountType.Basic);
            a1.Deposit(100000);

            BasicAccount from = (BasicAccount)Account.GetAccounts(conn).First(a => a._Owner.Id == user1 && a.AccountType == AccountType.Basic);
            var to = Account.GetAccounts(conn).First(a => a._Owner.Id == user2 && a.AccountType == AccountType.Savings);

            // Basic accounts have a max payment limit of 10000
            bool ok = from.SendPayment(to, 20000);

            Assert.IsFalse(ok);
        }

    }
}
