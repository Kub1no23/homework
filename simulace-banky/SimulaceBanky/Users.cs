using Microsoft.Data.Sqlite;
using System.Xml.Linq;

namespace SimulaceBanky.Users
{
    public abstract class User : IHasConnection
    {
        public SqliteConnection Connection { get; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public Roles Role { get; set; }
        public string Login { get; set; }

        public User(SqliteConnection connection) {
            this.Connection = connection;
        }

        public static User? GetAndVerify(SqliteConnection conn, string login, string password)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT 
                    Id,
                    Name,
                    Surname,
                    Role,
                    Password
                FROM Users
                WHERE Login = $login";
                cmd.Parameters.AddWithValue("$login", login);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    Roles role = Enum.Parse<Roles>(reader.GetString(3));
                    User user = role switch
                    {
                        Roles.Admin => new Admin(conn),
                        Roles.Banker => new Banker(conn),
                        Roles.Client => new Client(conn),
                        _ => throw new Exception("Unknown role in database")
                    };
                    user.Id = reader.GetInt32(0);
                    user.Name = reader.GetString(1);
                    user.Surname = reader.GetString(2);
                    string storedPass = reader.GetString(4);
                    user.Role = role;
                    user.Login = login;

                    if (!Helper.VerifyPassword(password, storedPass))
                        return null;

                    return user;
                }
            }
        }
        public static User[]? GetUsers(SqliteConnection conn, int? userId = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                if (userId == null)
                    cmd.CommandText = "SELECT Id, Name, Surname, Role, Login FROM Users;";
                else
                {
                    cmd.CommandText = @"
                    SELECT Id, Name, Surname, Role, Login 
                    FROM Users 
                    WHERE Id = $uid;";
                    cmd.Parameters.AddWithValue("$uid", userId);
                }

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    List<User> users = new List<User>();

                    while (reader.Read())
                    {
                        Roles role = Enum.Parse<Roles>(reader.GetString(3));

                        User user = role switch
                        {
                            Roles.Admin => new Admin(conn),
                            Roles.Banker => new Banker(conn),
                            Roles.Client => new Client(conn),
                            _ => throw new Exception("Unknown role in database")
                        };
                        user.Id = reader.GetInt32(0);
                        user.Name = reader.GetString(1);
                        user.Surname = reader.GetString(2);
                        user.Role = role;
                        user.Login = reader.GetString(4);

                        users.Add(user);
                    }

                    return users.Count == 0 ? null : users.ToArray();
                }
            }
        }
    }
    public class Admin : User
    {
        public Admin(SqliteConnection connection) : base(connection)
        {
        }

        public void CreateUser(string name, string surname, Roles role, string login, string password)
        {
            Console.WriteLine("Creating new user...");

            string hashPass = Helper.HashPassword(password);

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"
                INSERT INTO Users (Name, Surname, Role, Login, Password) VALUES
                ($name, $surname, $role, $login, $password);";
                cmd.Parameters.AddWithValue("$name", name);
                cmd.Parameters.AddWithValue("$surname", surname);
                cmd.Parameters.AddWithValue("$role", role.ToString());
                cmd.Parameters.AddWithValue("$login", login);
                cmd.Parameters.AddWithValue("$password", hashPass);

                try
                {
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid();";
                    int newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                    Logger.LogUserCreation(this.Id, newUserId, role.ToString());

                    Console.WriteLine($"User {name} {surname} with role {role} created successfully.");
                }
                catch (SqliteException ex) 
                {
                    Console.WriteLine($"Error: A user with the login '{login}' already exists.");
                }
            }
        }
        public void EditUser(int userId, string? name = null, string? surname = null, Roles? role = null, string? login = null, string? password = null)
        {
            Console.WriteLine("Editing user...");

            List<string> updates = new List<string>();
            if (name != null) updates.Add("Name = $name");
            if (surname != null) updates.Add("Surname = $surname");
            if (role != null) updates.Add("Role = $role");
            if (login != null) updates.Add("Login = $login");
            if (password != null) updates.Add("Password = $password");

            if (updates.Count == 0)
            {
                Console.WriteLine("No changes provided.");
                return;
            }

            string updateSql = string.Join(", ", updates);

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = $@"
                UPDATE Users
                SET {updateSql}
                WHERE Id = $id;
                ";
                cmd.Parameters.AddWithValue("$id", userId);

                if (name != null) cmd.Parameters.AddWithValue("$name", name);
                if (surname != null) cmd.Parameters.AddWithValue("$surname", surname);
                if (role != null) cmd.Parameters.AddWithValue("$role", role.ToString());
                if (login != null) cmd.Parameters.AddWithValue("$login", login);
                if (password != null) cmd.Parameters.AddWithValue("$password", password);

                try
                {
                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                        Console.WriteLine("No user found with that ID.");
                    else
                        Console.WriteLine("User updated successfully.");
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine("Database error: " + ex.Message);
                }
            }
        }
    }
    public class Banker : User
    {
        public Banker(SqliteConnection connection) : base(connection)
        {
        }

        public void OpenAccount(AccountType accType, int userId)
            => Account.OpenAccount(Connection, accType, userId, this.Id);
        public Account[]? GetAccounts(Client client)
            => Account.GetAccounts(Connection, client);
        public Account[]? GetAccounts()
            => Account.GetAccounts(Connection);
        public Dictionary<Account, decimal>? GetInterests()
            => Account.CalculateAccountsInterest(this.Connection, time: DateTime.Now);
    }
    public class Client : User
    {
        public Client(SqliteConnection connection) : base(connection)
        {
        }

        public Account[]? GetAccounts()
            => Account.GetAccounts(Connection, this);
        public void Deposit(Account acc, decimal amount)
            => acc.Deposit(amount);
        public void Withdraw(Account acc, decimal amount)
            => acc.Withdraw(amount);
        public void Transfer(ITransferable fromAcc, Account toAcc, decimal amount)
            => fromAcc.TransferFunds(toAcc, amount);
        public void Pay(IPayable fromAcc, Account toAcc, decimal amount)
            => fromAcc.SendPayment(toAcc, amount);
        public List<LogEntry> GetTransactions()
            => Logger.GetTransactions(this.Id);
    }
}
