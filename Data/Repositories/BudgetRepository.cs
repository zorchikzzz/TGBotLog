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
                Name TEXT NOT NULL UNIQUE,
                Type INTEGER NOT NULL DEFAULT 0,
                Color TEXT DEFAULT '#3498db',
                Icon TEXT DEFAULT '📁'
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

        public int AddCategory(string name, TransactionType type = TransactionType.Expense, string color = "#3498db", string icon = "📁")
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories (Name, Type, Color, Icon) VALUES ($name, $type, $color, $icon)";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$type", (int)type);
            cmd.Parameters.AddWithValue("$color", color);
            cmd.Parameters.AddWithValue("$icon", icon);
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
            cmd.CommandText = "SELECT Id, Name, Type, Color, Icon FROM Categories";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = (TransactionType)reader.GetInt32(2),
                    Color = reader.IsDBNull(3) ? "#3498db" : reader.GetString(3),
                    Icon = reader.IsDBNull(4) ? "📁" : reader.GetString(4)
                });
            }
            return categories;
        }

        public Category? GetCategoryByName(string name)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Type, Color, Icon FROM Categories WHERE Name = $name";
            cmd.Parameters.AddWithValue("$name", name);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = (TransactionType)reader.GetInt32(2),
                    Color = reader.IsDBNull(3) ? "#3498db" : reader.GetString(3),
                    Icon = reader.IsDBNull(4) ? "📁" : reader.GetString(4)
                };
            }
            return null;
        }

        /// <summary>
        /// Получение категории по ID
        /// </summary>
        /// <param name="id">ID категории для поиска</param>
        /// <returns>Найденная категория или null, если категория не найдена</returns>
        public Category? GetCategoryById(int id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Type, Color, Icon FROM Categories WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = (TransactionType)reader.GetInt32(2),
                    Color = reader.IsDBNull(3) ? "#3498db" : reader.GetString(3),
                    Icon = reader.IsDBNull(4) ? "📁" : reader.GetString(4)
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
                // Получаем категорию для определения типа транзакции
                var category = GetCategoryById(reader.GetInt32(3));

                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    Amount = reader.GetDecimal(1),
                    Date = DateTime.Parse(reader.GetString(2)),
                    CategoryId = reader.GetInt32(3),
                    Description = reader.GetString(4),
                    Type = category?.Type ?? TransactionType.Expense
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