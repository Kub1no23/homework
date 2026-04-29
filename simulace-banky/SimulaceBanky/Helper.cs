using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace SimulaceBanky
{
    public enum MenuResult
    {
        Exit,
        Logout
    }
    public enum Roles
    {
        Admin,
        Banker,
        Client
    }
    public enum AccountType
    {
        Basic,
        Savings,
        StudentSavings,
        Credit
    }
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer,
        Payment
    }

    public interface IHasConnection
    {
        SqliteConnection Connection { get; }
    }
    public interface IWithdrawable
    {
        public decimal? _DailyLimit { get; }
        public decimal? _MaxPaymentLimit { get; }
        public bool Withdraw(decimal amount);
    }
    public interface IPayable
    {
        internal bool SendPayment(Account to, decimal amount);
    }
    public interface ITransferable
    {
        internal bool TransferFunds(Account to, decimal amount);
    }
    public static class Helper
    {
        public static void ShiftLinesUp(int offset, int line, string? s = null)
        {
            int currentLine = line;

            for (int i = offset; i >= 0; i--)
            {
                int maxLevel = currentLine - i - 1;
                if (maxLevel < 0)
                    continue;

                if (i == offset && s != null)
                {
                    Console.SetCursorPosition(0, currentLine - i - 1);
                    Console.Write(s + new string(' ', Console.WindowWidth - s.Length));
                    continue;
                }

                Console.SetCursorPosition(0, currentLine - i - 1);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            if (s == null)
                Console.SetCursorPosition(0, line - offset - 1);
            else
                Console.SetCursorPosition(0, line - offset);
        }
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
        public static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
    }
}
