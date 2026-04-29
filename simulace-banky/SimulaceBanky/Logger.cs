using Microsoft.Data.Sqlite;
using SimulaceBanky.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulaceBanky
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Timestamp { get; set; }
        public int? InitiatorId { get; set; }
        public int? UserId { get; set; }
        public int? SourceAccountId { get; set; }
        public int? TargetAccountId { get; set; }
        public decimal? Amount { get; set; }
        public string Message { get; set; }
    }
    public static class Logger
    {
        private static SqliteConnection _connection;

        public static void Initialize(SqliteConnection connection)
        {
            _connection = connection;
        }

        //Set
        private static void Log(
            string type,
            int? initiatorId = null,
            int? userId = null,
            int? targetAccountId = null,
            int? sourceAccountId = null,
            decimal? amount = null,
            string message = null)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO Logs (Type, InitiatorId, UserId, TargetAccountId, SourceAccountId, Amount, Message)
            VALUES (@type, @initiatorId, @userId, @targetAccountId, @sourceAccountId, @amount, @message);
        ";

            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@initiatorId", (object?)initiatorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@targetAccountId", (object?)targetAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sourceAccountId", (object?)sourceAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", (object?)amount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@message", (object?)message ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
        public static void LogLogin(string login, bool success, bool lockedOut)
        {
            int? userId = null;
            string message;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Id FROM Users WHERE Login = @login";
                cmd.Parameters.AddWithValue("@login", login);

                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    userId = Convert.ToInt32(result);
                }
            }
            message = userId == null ? "User not found" : success ? "Login success" : lockedOut ? "Incorrect password (account locked)" : "Incorrect password";

            Log(
                type: "Login",
                initiatorId: success ? userId : null,
                userId: userId,
                message: message
            );
        }
        public static void LogUserCreation(int adminId, int newUserId, string role)
        {
            Log(
                type: "CreateUser",
                initiatorId: adminId,
                userId: newUserId,
                message: $"Created user with role {role}"
            );
        }
        public static void LogAccountCreation(
            int bankerId,
            int userId,
            int accountId,
            string accountType,
            decimal initialBalance)
        {
            Log(
                type: "CreateAccount",
                initiatorId: bankerId,
                userId: userId,
                targetAccountId: accountId,
                amount: initialBalance,
                message: $"Created {accountType} account"
            );
        }
        public static void LogTransaction(
            TransactionType transType,
            int initiatorId,
            int userId,
            int targetAccountId,
            int sourceAccountId,
            decimal amount,
            string message)
        {
            Log(
                type: transType.ToString(),
                initiatorId: initiatorId,
                userId: userId,
                targetAccountId: targetAccountId,
                sourceAccountId: sourceAccountId,
                amount: amount,
                message: message
            );
        }

        //Get
        private static LogEntry ReadLog(SqliteDataReader r)
        {
            return new LogEntry
            {
                Id = Convert.ToInt32(r["Id"]),
                Type = r["Type"].ToString(),
                Timestamp = r["Timestamp"].ToString(),

                InitiatorId = r["InitiatorId"] == DBNull.Value ? null : Convert.ToInt32(r["InitiatorId"]),
                UserId = r["UserId"] == DBNull.Value ? null : Convert.ToInt32(r["UserId"]),
                SourceAccountId = r["SourceAccountId"] == DBNull.Value ? null : Convert.ToInt32(r["SourceAccountId"]),
                TargetAccountId = r["TargetAccountId"] == DBNull.Value ? null : Convert.ToInt32(r["TargetAccountId"]),

                Amount = r["Amount"] == DBNull.Value ? null : Convert.ToDecimal(r["Amount"]),

                Message = r["Message"] == DBNull.Value ? null : r["Message"].ToString()
            };
        }

        public static List<LogEntry> GetTransactions(int? userId = null, bool filterInterestAcc = false, DateTime? start = null, DateTime? end = null)
        {
            List<LogEntry> list = new List<LogEntry>();

            using var cmd = _connection.CreateCommand();

            string baseSql = @"
                SELECT Logs.*
                FROM Logs
            ";

            if (filterInterestAcc)
            {
                baseSql += @"
                JOIN Accounts 
                    ON Accounts.Id = Logs.SourceAccountId 
                    OR Accounts.Id = Logs.TargetAccountId
                JOIN AccountTypes
                    ON AccountTypes.Id = Accounts.AccountTypeId
                ";
            }

            baseSql += @"
                WHERE Logs.Type IN ('Deposit', 'Withdrawal', 'Transfer', 'Payment')
            ";
            if (start != null)
            {
                baseSql += " AND Logs.Timestamp >= $start ";
                cmd.Parameters.AddWithValue("$start", start.Value);
            }
            if (end != null)
            {
                baseSql += " AND Logs.Timestamp <= $end ";
                cmd.Parameters.AddWithValue("$end", end.Value);
            }
            if (userId != null)
            {
                baseSql += " AND Logs.UserId = $userId ";
                cmd.Parameters.AddWithValue("$userId", userId);
            }
            if (filterInterestAcc)
            {
                baseSql += " AND AccountTypes.Name IN ('Basic', 'Savings', 'StudentSavings') ";
            }

            baseSql += " ORDER BY Logs.Timestamp DESC;";

            cmd.CommandText = baseSql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadLog(reader));
            }

            return list;
        }

        public static List<LogEntry> GetActivity()
        {
            List<LogEntry> list = new List<LogEntry>();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
            SELECT * FROM Logs
            WHERE Type NOT IN ('Deposit', 'Withdrawal', 'Transfer', 'Payment')
            ORDER BY Timestamp DESC;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadLog(reader));
            }

            return list;
        }
    }

}
