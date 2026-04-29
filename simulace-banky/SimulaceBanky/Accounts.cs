using Microsoft.Data.Sqlite;
using SimulaceBanky.Users;
using System.Security.AccessControl;

namespace SimulaceBanky
{
    public abstract class Account : IHasConnection, IWithdrawable
    {
        public SqliteConnection? Connection { get; }
        public int Id { get; protected set; }
        public AccountType AccountType { get; protected set; }
        public decimal Balance { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public decimal? _DailyLimit { get; set; }
        public decimal? _MaxPaymentLimit { get; set; }
        public Client? _Owner { get; }

        public Account(int id) {
            this.Id = id;
        }
        public Account(SqliteConnection connection, int id, AccountType accType, decimal balance, DateTime createdAt, decimal? dailyLimit, decimal? maxPayLimit, Client? owner)
        {
            this.Connection = connection;
            this.Id = id;
            this.AccountType = accType;
            this.Balance = balance;
            this.CreatedAt = createdAt;
            this._DailyLimit = dailyLimit;
            this._MaxPaymentLimit = maxPayLimit;
            this._Owner = owner;
        }

        private static int GetAccountTypeId(SqliteConnection conn, AccountType accType)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT Id FROM AccountTypes
                WHERE Name = $name;
                ";
                cmd.Parameters.AddWithValue("$name", accType.ToString());

                object? result = cmd.ExecuteScalar();

                if (result == null)
                    throw new Exception("Invalid account type");

                return Convert.ToInt32(result);
            }
        }
        public static void OpenAccount(SqliteConnection conn, AccountType accType, int userId, int bankerId)
        {
            Console.WriteLine($"Opening new account for user[{userId}]...");

            decimal dailyLimit = Bank.GetMaxDailyWithdraw(accType);
            decimal maxPaymentLimit = Bank.GetMaxWithdraw(accType);
            decimal? creditLimit = accType == AccountType.Credit ? Bank.CreditLimit : null;

            int accTypeId = GetAccountTypeId(conn, accType);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Accounts 
                        (AccountTypeId, UserId, Balance, DailyLimit, MaxPaymentLimit, CreditLimit)
                    VALUES 
                        ($typeId, $userId, 0, $dailyLimit, $maxPayLimit, $creditLimit);";
                cmd.Parameters.AddWithValue("$typeId", accTypeId);
                cmd.Parameters.AddWithValue("$userId", userId);
                cmd.Parameters.AddWithValue("$dailyLimit", dailyLimit);
                cmd.Parameters.AddWithValue("$maxPayLimit", (object?)maxPaymentLimit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$creditLimit", (object?)creditLimit ?? DBNull.Value);

                try
                {
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT last_insert_rowid();";
                    int newAccountId = Convert.ToInt32(cmd.ExecuteScalar());

                    Logger.LogAccountCreation(
                        bankerId,
                        userId,
                        accountId: newAccountId,
                        accountType: accType.ToString(),
                        initialBalance: 0
                    );
                    Console.WriteLine($"Account of type {accType.ToString()} for user created successfully.");
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine("Database error: " + ex.Message);
                }
            }
        }
        public static Account[]? GetAccounts(SqliteConnection conn, Client? client = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                if (client == null)
                {
                    cmd.CommandText = @"
                    SELECT 
                        Accounts.Id,
                        Accounts.AccountTypeId,
                        AccountTypes.Name AS AccountTypeName,
                        Accounts.UserId,
                        Accounts.Balance,
                        Accounts.CreatedAt,
                        Accounts.DailyLimit,
                        Accounts.MaxPaymentLimit,
                        Accounts.CreditLimit
                    FROM Accounts
                    JOIN AccountTypes ON Accounts.AccountTypeId = AccountTypes.Id;";
                }
                else
                {
                    cmd.CommandText = @"
                    SELECT 
                        Accounts.Id,
                        Accounts.AccountTypeId,
                        AccountTypes.Name AS AccountTypeName,
                        Accounts.UserId,
                        Accounts.Balance,
                        Accounts.CreatedAt,
                        Accounts.DailyLimit,
                        Accounts.MaxPaymentLimit,
                        Accounts.CreditLimit
                    FROM Accounts
                    JOIN AccountTypes ON Accounts.AccountTypeId = AccountTypes.Id
                    WHERE Accounts.UserId = $uid;";
                    cmd.Parameters.AddWithValue("$uid", client.Id);
                }

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    List<Account> accounts = new List<Account>();

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        if (id == 0) continue; //Bank Reserve Acount

                        int typeId = reader.GetInt32(1);
                        string typeName = reader.GetString(2);
                        int clientId = reader.GetInt32(3);
                        double balance = reader.GetDouble(4);
                        string createdAt = reader.GetString(5);
                        double? dailyLimit = reader.IsDBNull(6) ? null : reader.GetDouble(6);
                        double? maxPayLimit = reader.IsDBNull(7) ? null : reader.GetDouble(7);
                        double? creditLimit = reader.IsDBNull(8) ? null : reader.GetDouble(8);

                        User[]? users = User.GetUsers(conn, clientId);
                        if (users != null)
                        {
                            client = (Client)users[0];
                        }

                        Account acc = Enum.Parse<AccountType>(typeName) switch
                        {
                            AccountType.Basic => new BasicAccount(conn, id, Enum.Parse<AccountType>(typeName), (decimal)balance, DateTime.Parse(createdAt), (decimal)dailyLimit, (decimal)maxPayLimit, client),
                            AccountType.Credit => new CreditAccount(conn, id, Enum.Parse<AccountType>(typeName), (decimal)balance, DateTime.Parse(createdAt), (decimal)dailyLimit, (decimal)maxPayLimit, (decimal)creditLimit, client),
                            AccountType.Savings => new SavingsAccount(conn, id, Enum.Parse<AccountType>(typeName), (decimal)balance, DateTime.Parse(createdAt), (decimal)dailyLimit, (decimal)maxPayLimit, client),
                            AccountType.StudentSavings => new StudentsSavingsAccount(conn, id, Enum.Parse<AccountType>(typeName), (decimal)balance, DateTime.Parse(createdAt), (decimal)dailyLimit, (decimal)maxPayLimit, client),
                            _ => throw new Exception("Unknown account type")
                        };

                        accounts.Add(acc);
                    }

                    return accounts.Count == 0 ? null : accounts.ToArray();
                }
            }
        }
        public static bool ChangeBalance(Account from, Account to, decimal amount)
        {
            var conn = from.Connection ?? to.Connection;
            if (conn == null)
            {
                throw new InvalidOperationException("Both accounts have null Connection.");
            }

            if (amount > from._MaxPaymentLimit)
            {
                Console.WriteLine($"Your maximum payment limit is {from._MaxPaymentLimit}");
                return false;
            }

            DateTime start = DateTime.Today;
            DateTime end = DateTime.Today.AddDays(1).AddTicks(-1);
            List<LogEntry> logs = Logger.GetTransactions(from._Owner?.Id, false, start, end);
            decimal dailySum = logs
                .Where(l => l.SourceAccountId == from.Id)
                .Sum(l => l.Amount ?? 0);
            if (dailySum + amount > from._DailyLimit)
            {
                Console.WriteLine($"Your daily maximum payment limit is {from._DailyLimit}");
                return false;
            }


            using (SqliteTransaction transaction = conn.BeginTransaction())
            {
                try
                {
                    //sender
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        UPDATE Accounts
                        SET Balance = Balance - $amount
                        WHERE Id = $id;
                        ";
                        cmd.Parameters.AddWithValue("$amount", amount);
                        cmd.Parameters.AddWithValue("$id", from.Id);
                        cmd.ExecuteNonQuery();
                    }
                    //receiver
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        UPDATE Accounts
                        SET Balance = Balance + $amount
                        WHERE Id = $id;
                        ";
                        cmd.Parameters.AddWithValue("$amount", amount);
                        cmd.Parameters.AddWithValue("$id", to.Id);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    Console.WriteLine($"Payment of {amount} sent successfully. Account[{from.Id}] -> Account[{to.Id}]");

                    return true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Database error: " + ex.Message);
                    return false;
                }
            }
        }
        public virtual bool Withdraw(decimal amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Withdrawal amount must be greater than zero.");
                return false;
            }

            bool success = ChangeBalance(this, Bank.VaultAccount, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Withdrawal, this._Owner.Id, this._Owner.Id, this.Id, Bank.VaultAccount.Id, amount, $"Withdrawal of {amount}");
                return success;
            }

            return false;
        }
        public virtual bool Deposit(decimal amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Deposit amount must be greater than zero.");
                return false;
            }

            bool success = ChangeBalance(Bank.VaultAccount, this, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Deposit, this._Owner.Id, this._Owner.Id, Bank.VaultAccount.Id, this.Id, amount, $"Deposit of {amount}");
                return success;
            }

            return false;
        }
        public static Dictionary<Account, decimal>? CalculateAccountsInterest(SqliteConnection conn, Client? client = null, DateTime? time = null)
        {
            Dictionary<int, Dictionary<DateTime, decimal>> accountsBalances = new Dictionary<int, Dictionary<DateTime, decimal>>();

            DateTime t = time ?? DateTime.Now;
            int year = t.Year;
            int month = t.Month;
            DateTime start = new DateTime(year, month, 1);
            DateTime end = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            List<LogEntry> logs = Logger.GetTransactions(client?.Id, true, start, end).OrderByDescending(l => DateTime.Parse(l.Timestamp)).ToList();

            foreach (LogEntry log in logs)
            {
                DateTime date = DateTime.Parse(log.Timestamp);
                if (log.Amount is null)
                    throw new InvalidOperationException("Log entry has null Amount, which should never happen in this context.");

                if (log.SourceAccountId is int srcId)
                {
                    if (!accountsBalances.ContainsKey(srcId))
                        accountsBalances[srcId] = new Dictionary<DateTime, decimal>();

                    if (!accountsBalances[srcId].ContainsKey(date))
                        accountsBalances[srcId][date] = 0;

                    accountsBalances[srcId][date] -= log.Amount.Value;
                }
                if (log.TargetAccountId is int tarId)
                {
                    if (!accountsBalances.ContainsKey(tarId))
                        accountsBalances[tarId] = new Dictionary<DateTime, decimal>();

                    if (!accountsBalances[tarId].ContainsKey(date))
                        accountsBalances[tarId][date] = 0;

                    accountsBalances[tarId][date] += log.Amount.Value;
                }
            }

            Account[]? allAccounts = GetAccounts(conn, client);
            if (allAccounts == null)
            {
                Console.WriteLine("No accounts found for interest calculation.");
                return null;
            }

            var relevantAccounts = allAccounts
                .Where(a => accountsBalances.ContainsKey(a.Id))
                .ToDictionary(a => a.Id, a => a.Balance);

            foreach (int accId in accountsBalances.Keys)
            {
                decimal currBalance = relevantAccounts[accId];

                var dates = accountsBalances[accId]
                    .Keys
                    .OrderByDescending(d => d)
                    .ToList();

                if (!accountsBalances[accId].ContainsKey(end))
                {
                    accountsBalances[accId][end] = currBalance;
                }
                foreach (DateTime date in dates)
                {
                    decimal delta = accountsBalances[accId][date];

                    accountsBalances[accId][date] = currBalance;

                    currBalance -= delta;
                }
                if (!accountsBalances[accId].ContainsKey(start))
                {
                    accountsBalances[accId][start] = currBalance;
                }
            }

            Dictionary<Account, decimal> accInterests = new Dictionary<Account, decimal>();
            foreach (int accId in accountsBalances.Keys)
            {
                Account acc = allAccounts.First(a => a.Id == accId);
                decimal interest = Bank.CalculateMonthlyInterest(acc.AccountType, accountsBalances[accId]);
                accInterests[acc] = interest;
            }

            return accInterests;
        }

    }
    public class  ReserveAccount : Account
    {
        public ReserveAccount(int id) : base(id) {}
    }
    public class BasicAccount : Account, IPayable, ITransferable
    {
        public BasicAccount(SqliteConnection connection, int id, AccountType accType, decimal balance, DateTime createdAt, decimal? dailyLimit, decimal? maxPayLimit, Client? owner) 
            : base(connection, id, accType, balance, createdAt, dailyLimit, maxPayLimit, owner) {}

        public bool TransferFunds(Account to, decimal amount)
        {
            if (this._Owner.Id != to._Owner.Id)
            {
                Console.WriteLine("Transfers between different clients are not allowed.");
                return false;
            }
            if (this.Balance < amount)
            {
                Console.WriteLine("Insufficient funds.");
                return false;
            }

            bool success = ChangeBalance(this, to, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Transfer, this._Owner.Id, this._Owner.Id, to.Id, this.Id, amount, $"Transfer of {amount} to account[{to.Id}]");
                return success;
            }

            return false;
        }
        public bool SendPayment(Account to, decimal amount)
        {
            if (this._Owner.Id == to._Owner.Id)
            {
                Console.WriteLine("You can't send a payment to yourself.");
                return false;
            }
            if (this.Balance < amount)
            {
                Console.WriteLine("Insufficient funds.");
                return false;
            }
            bool success = ChangeBalance(this, to, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Payment, this._Owner.Id, this._Owner.Id, to.Id, this.Id, amount, $"Payment of {amount} to account[{to.Id}]");
                return success;
            }

            return false;
        }
        public override bool Withdraw(decimal amount)
        {
            if (this.Balance < amount)
            {
                Console.WriteLine("Insufficient funds.");
                return false;
            }

            return base.Withdraw(amount);
        }
    }
    public class CreditAccount : Account, IPayable
    {
        public decimal _CreditLimit { get; }
        public CreditAccount(SqliteConnection connection, int id, AccountType accType, decimal balance, DateTime createdAt, decimal? dailyLimit, decimal? maxPayLimit, decimal creditLimit, Client? owner) 
            : base(connection, id, accType, balance, createdAt, dailyLimit, maxPayLimit, owner) { 
            this._CreditLimit = creditLimit;
        }

        public bool SendPayment(Account to, decimal amount)
        {
            if (this._Owner.Id == to._Owner.Id)
            {
                Console.WriteLine("You can't send a payment to yourself.");
                return false;
            }
            if (this.Balance - amount < -this._CreditLimit)
            {
                Console.WriteLine("The payment exceeds credit card limit.");
                return false;
            }
            bool success = ChangeBalance(this, to, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Payment, this._Owner.Id, this._Owner.Id, to.Id, this.Id, amount, $"Payment of {amount} to account[{to.Id}]");
                return success;
            }

            return false;
        }
        public override bool Withdraw(decimal amount)
        {
            if (this.Balance - amount < -this._CreditLimit)
            {
                Console.WriteLine("The withdrawal exceeds credit card limit.");
                return false;
            }

            return base.Withdraw(amount);
        }
    }
    public class SavingsAccount : Account, ITransferable
    {
        public SavingsAccount(SqliteConnection connection, int id, AccountType accType, decimal balance, DateTime createdAt, decimal? dailyLimit, decimal? maxPayLimit, Client? owner) 
            : base(connection, id, accType, balance, createdAt, dailyLimit, maxPayLimit, owner) {}

        public bool TransferFunds(Account to, decimal amount)
        {
            if (this._Owner.Id != to._Owner.Id)
            {
                Console.WriteLine("Transfers between different clients are not allowed.");
                return false;
            }
            if (this.Balance < amount)
            {
                Console.WriteLine("Insufficient funds.");
                return false;
            }

            bool success = ChangeBalance(this, to, amount);

            if (success)
            {
                Logger.LogTransaction(TransactionType.Transfer, this._Owner.Id, this._Owner.Id, to.Id, this.Id, amount, $"Transfer of {amount} to account[{to.Id}]");
                return success;
            }

            return false;
        }
    }
    public class StudentsSavingsAccount : SavingsAccount
    {
        public StudentsSavingsAccount(SqliteConnection connection, int id, AccountType accType, decimal balance, DateTime createdAt, decimal? dailyLimit, decimal? maxPayLimit, Client? owner) 
            : base(connection, id, accType, balance, createdAt, dailyLimit, maxPayLimit, owner) {}
    }
}
