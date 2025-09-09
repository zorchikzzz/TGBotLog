using Microsoft.Data.Sqlite;
using FamilyBudgetBot.Data.Models;
using System.Globalization;

namespace FamilyBudgetBot.Data.Repositories
{
    public class BudgetRepository : IDisposable
    {
        private readonly string _connectionString;

        public BudgetRepository(string dbPath = "budget.db")
        {
            _connectionString = $"Data Source={dbPath};Pooling=False;";
            InitializeDatabase();
        }

        private SqliteConnection GetOpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void InitializeDatabase()
        {
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();

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
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
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
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
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
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
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
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
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

        public Category? GetCategoryById(int id)
        {
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
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

        public List<Transaction> GetTransactions(DateTime? startDate = null, DateTime? endDate = null, 
            bool last10 = false )
        {
            var transactions = new List<Transaction>();
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();

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

            if (last10 == true)
            {
                cmd.CommandText += " ORDER BY Date DESC LIMIT (10)";
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
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
        
        // В BudgetRepository.cs
        public Dictionary<int, List<int>> GetTransactionYearsMonths()
        {
            var result = new Dictionary<int, List<int>>();
            
            using var connection = GetOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Date FROM Transactions ORDER BY Date DESC";
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (DateTime.TryParse(reader.GetString(0), out DateTime date))
                {
                    if (!result.ContainsKey(date.Year))
                        result[date.Year] = new List<int>();
                        
                    if (!result[date.Year].Contains(date.Month))
                        result[date.Year].Add(date.Month);
                }
            }
            
            // Сортируем года по убыванию, а месяцы по возрастанию внутри года
            return result
                .OrderByDescending(x => x.Key)
                .ToDictionary(
                    y => y.Key, 
                    y => y.Value.OrderBy(m => m).ToList()
                );
        }

        public void Dispose()
        {

        }
       
    }
}