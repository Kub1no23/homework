using SimulaceBanky.Users;
using Microsoft.Data.Sqlite;

namespace SimulaceBanky
{
    public class DB : IHasConnection
    {
        private SqliteConnection _connection;
        public SqliteConnection Connection 
        {
            get => _connection;
            set {
                if (value != null)
                {
                    _connection = value;
                }
            } 
        }
        public DB(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string cannot be empty.");

            this.Connection = new SqliteConnection(connectionString);
        
            Connection.Open();
            Console.WriteLine("Initiated connection with the DB.");

            using (var command = Connection.CreateCommand())
            {
                string createUserTable = @"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Surname TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    Login TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL
                );";
                command.CommandText = createUserTable;
                command.ExecuteNonQuery();

                string aPass = Helper.HashPassword("admin");
                string bPass = Helper.HashPassword("banker");
                string cPass = Helper.HashPassword("client");
                string insertUserAdmin = @"
                INSERT INTO Users (Name, Surname, Role, Login, Password) VALUES
                ('Admin', 'Admin', 'Admin', 'admin', $aPass),
                ('Banker', 'Banker', 'Banker', 'banker', $bPass),
                ('Client', 'Client', 'Client', 'client', $cPass);";
                command.CommandText = insertUserAdmin;
                command.Parameters.AddWithValue("$aPass", aPass);
                command.Parameters.AddWithValue("$bPass", bPass);
                command.Parameters.AddWithValue("$cPass", cPass);
                command.ExecuteNonQuery();

                string createAccountTypesTable = @"
                CREATE TABLE AccountTypes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );";
                command.CommandText = createAccountTypesTable;
                command.ExecuteNonQuery();

                string insertAccountTypes = @"
                INSERT INTO AccountTypes (Name) VALUES
                ('Basic'),
                ('Savings'),
                ('StudentSavings'),
                ('Credit');";
                command.CommandText = insertAccountTypes;
                command.ExecuteNonQuery();

                string createAccountsTable = @"
                CREATE TABLE Accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccountTypeId INTEGER,
                    UserId INTEGER,
                    Balance REAL NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),

                    DailyLimit REAL,
                    MaxPaymentLimit REAL,
                    CreditLimit REAL,

                    FOREIGN KEY (AccountTypeId) REFERENCES AccountTypes(Id),
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );";
                command.CommandText = createAccountsTable;
                command.ExecuteNonQuery();

                string insertBankAccount = @"
                INSERT INTO Accounts (
                    Id, AccountTypeId, UserId, Balance, DailyLimit, MaxPaymentLimit, CreditLimit
                ) VALUES (
                    0, NULL, NULL, 20000000, NULL, NULL, NULL);";
                command.CommandText = insertBankAccount;
                command.ExecuteNonQuery();


                string createLogsTable = @"
                CREATE TABLE Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Type TEXT NOT NULL,
                    Timestamp TEXT NOT NULL DEFAULT (datetime('now')),
                    InitiatorId INTEGER,
                    UserId INTEGER,
                    TargetAccountId INTEGER,
                    SourceAccountId INTEGER,
                    Amount REAL,
                    Message TEXT,

                    FOREIGN KEY (InitiatorId) REFERENCES Users(Id),
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (TargetAccountId) REFERENCES Accounts(Id)
                );";
                command.CommandText = createLogsTable;
                command.ExecuteNonQuery();

                Logger.Initialize(Connection);
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            DB db = new DB("Data Source=:memory:");

            int tries = 3;
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.Write("Welcome to the Bank Simulation!\n\nPress any key to continue.\n\n");
                Console.ReadKey(intercept:true);
                int currentLine = Console.CursorTop;
                Helper.ShiftLinesUp(1, currentLine);

                Console.Write("Login: ");
                string login = Console.ReadLine() ?? "";
                if (login == "") return;
                currentLine = Console.CursorTop;
                Helper.ShiftLinesUp(0, currentLine, login);

                Console.Write("Password: ");
                string password = Console.ReadLine() ?? "";
                if (password == "") return;
                currentLine = Console.CursorTop;
                Helper.ShiftLinesUp(0, currentLine, password);

                User user = User.GetAndVerify(db.Connection, login, password);
                if (user == null) 
                {
                    tries--;
                    Console.WriteLine($"\nLogin failed. {tries} tries remaining.");
                    if (tries == 0)
                    {
                        Console.WriteLine("Too many failed attempts. Exiting.");
                        exit = true;
                    }
                    Logger.LogLogin(login, success: false, lockedOut: tries == 0);
                    Console.ReadKey(intercept: true);
                    continue;
                }
                Logger.LogLogin(login, success: true, lockedOut: false);
                tries = 3;

                switch (user.Role)
                {
                    case Roles.Admin:
                        Admin admin = (Admin)user;

                        MenuResult result = new AdminMenu(admin).Show();
                        if (result == MenuResult.Logout)
                            continue;
                        else if (result == MenuResult.Exit)
                        {
                            exit = true;
                            break;
                        }

                        break;
                    case Roles.Banker:
                        Banker banker = (Banker)user;

                        result = new BankerMenu(banker).Show();
                        if (result == MenuResult.Logout)
                            continue;
                        else if (result == MenuResult.Exit)
                        {
                            exit = true;
                            break;
                        }

                        break;
                    case Roles.Client:
                        Client client = (Client)user;

                        result = new ClientMenu(client).Show();
                        if (result == MenuResult.Logout)
                            continue;
                        else if (result == MenuResult.Exit)
                        {
                            exit = true;
                            break;
                        }

                        break;
                }
            }
        }
    }
}
