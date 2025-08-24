using Microsoft.Data.Sqlite;
using FamilyBudgetBot.Data.Models;
using System.Globalization;

namespace FamilyBudgetBot.Data.Repositories
{
    public class BudgetRepository : IDisposable
    {
        private readonly SqliteConnection _connection;

        public BudgetRepository(string dbPath = "budget.db")
        {
            _connection = new SqliteConnection($"Data Source={dbPath}");
            _connection.Open();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var cmd = _connection.CreateCommand();

            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            )";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount REAL NOT NULL,
                Date TEXT NOT NULL,
                CategoryId INTEGER NOT NULL,
                Description TEXT,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            )";
            cmd.ExecuteNonQuery();
        }

        // ƒобавл€ем новый метод дл€ поиска категории по имени
        public Category? GetCategoryByName(string name)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Name = $name";
            cmd.Parameters.AddWithValue("$name", name);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
            }
            return null;
        }

        // ќстальные методы остаютс€ без изменений
        public int AddCategory(string name)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories (Name) VALUES ($name)";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT last_insert_rowid()";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int AddTransaction(Transaction transaction)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO Transactions (Amount, Date, CategoryId, Description) 
            VALUES ($amount, $date, $categoryId, $description)";

            cmd.Parameters.AddWithValue("$amount", transaction.Amount);
            cmd.Parameters.AddWithValue("$date", transaction.Date.ToString("o"));
            cmd.Parameters.AddWithValue("$categoryId", transaction.CategoryId);
            cmd.Parameters.AddWithValue("$description", transaction.Description ?? "");
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT last_insert_rowid()";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return categories;
        }

        public Category? GetCategoryById(int id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
            }
            return null;
        }

        public List<Transaction> GetTransactions(DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = new List<Transaction>();
            using var cmd = _connection.CreateCommand();

            cmd.CommandText = @"
            SELECT t.Id, t.Amount, t.Date, t.CategoryId, t.Description 
            FROM Transactions t
            WHERE 1=1";

            if (startDate.HasValue)
            {
                cmd.CommandText += " AND Date >= $startDate";
                cmd.Parameters.AddWithValue("$startDate", startDate.Value.ToString("o"));
            }

            if (endDate.HasValue)
            {
                cmd.CommandText += " AND Date <= $endDate";
                cmd.Parameters.AddWithValue("$endDate", endDate.Value.ToString("o"));
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    Amount = reader.GetDecimal(1),
                    Date = DateTime.Parse(reader.GetString(2)),
                    CategoryId = reader.GetInt32(3),
                    Description = reader.GetString(4)
                });
            }
            return transactions;
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}