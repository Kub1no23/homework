using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimulaceBanky;

public abstract class TestBase
{
    protected DB db;
    protected SqliteConnection conn;

    [TestInitialize]
    public void Setup()
    {
        db = new DB("Data Source=:memory:");
        conn = db.Connection;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = OFF;";
        cmd.ExecuteNonQuery();
    }

    protected int CreateUser(string name, string surname, Roles role, string login, string password)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Users (Name, Surname, Role, Login, Password)
            VALUES ($n, $s, $r, $l, $p);
        ";
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$s", surname);
        cmd.Parameters.AddWithValue("$r", role.ToString());
        cmd.Parameters.AddWithValue("$l", login);
        cmd.Parameters.AddWithValue("$p", Helper.HashPassword(password));
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT last_insert_rowid();";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    protected decimal GetBalance(int accountId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Balance FROM Accounts WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", accountId);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }
}
