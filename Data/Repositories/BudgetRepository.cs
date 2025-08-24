using Microsoft.Data.Sqlite;
using FamilyBudgetBot.Data.Models;
using System.Globalization;

namespace FamilyBudgetBot.Data.Repositories
{
    /// <summary>
    /// ����������� ��� ������ � ����� ������ SQLite
    /// ������������ ��� �������� � �������: ��������, ������, ����������, ��������
    /// ��������� IDisposable ��� ����������� ������������ ��������
    /// </summary>
    public class BudgetRepository : IDisposable
    {
        // ���������� � ����� ������ SQLite
        private readonly SqliteConnection _connection;

        /// <summary>
        /// ����������� �����������
        /// </summary>
        /// <param name="dbPath">���� � ����� ���� ������ (�� ��������� "budget.db")</param>
        public BudgetRepository(string dbPath = "budget.db")
        {
            // ������� ���������� � SQLite ����� ������
            // Data Source ��������� �� ����, ��� �������� ������
            _connection = new SqliteConnection($"Data Source={dbPath}");

            // ��������� ���������� � ����� ������
            _connection.Open();

            // �������������� ��������� ���� ������ (������� �������, ���� ��� �� ����������)
            InitializeDatabase();
        }

        /// <summary>
        /// ������������� ��������� ���� ������
        /// ������� ����������� �������, ���� ��� �� ����������
        /// </summary>
        private void InitializeDatabase()
        {
            // ���������� ������� ��� ���������� SQL-��������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� �������� ������� ���������, ���� ��� �� ����������
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,    -- �������������������� ��������� ����
                Name TEXT NOT NULL UNIQUE               -- ���������� �������� ���������
            )";
            cmd.ExecuteNonQuery();  // ��������� ������

            // SQL-������ ��� �������� ������� ����������, ���� ��� �� ����������
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,    -- �������������������� ��������� ����
                Amount REAL NOT NULL,                   -- ����� ����������
                Date TEXT NOT NULL,                     -- ���� ���������� � ��������� �������
                CategoryId INTEGER NOT NULL,            -- ������� ���� � ������� Categories
                Description TEXT,                       -- �������� ���������� (����� ���� NULL)
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)  -- ����� � �������� Categories
            )";
            cmd.ExecuteNonQuery();  // ��������� ������
        }

        /// <summary>
        /// ����� ��������� �� �����
        /// </summary>
        /// <param name="name">�������� ��������� ��� ������</param>
        /// <returns>��������� ��������� ��� null, ���� ��������� �� �������</returns>
        public Category? GetCategoryByName(string name)
        {
            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� ������ ��������� �� �����
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Name = $name";

            // ��������� �������� � ������� ��� ������ �� SQL-��������
            cmd.Parameters.AddWithValue("$name", name);

            // ��������� ������ � �������� reader ��� ������ �����������
            using var reader = cmd.ExecuteReader();

            // ���� ���� ����������, ������� � ���������� ������ Category
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),        // ������ �������� ������� ������� (Id)
                    Name = reader.GetString(1)      // ������ �������� ������� ������� (Name)
                };
            }

            // ���� ��������� �� �������, ���������� null
            return null;
        }

        /// <summary>
        /// ���������� ����� ��������� � ���� ������
        /// </summary>
        /// <param name="name">�������� ����� ���������</param>
        /// <returns>ID ����������� ���������</returns>
        public int AddCategory(string name)
        {
            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� ������� ����� ���������
            cmd.CommandText = "INSERT INTO Categories (Name) VALUES ($name)";

            // ��������� �������� � ������� ��� ������ �� SQL-��������
            cmd.Parameters.AddWithValue("$name", name);

            // ��������� ������ (��������� ����� ������)
            cmd.ExecuteNonQuery();

            // �������� ID ��������� ����������� ������
            cmd.CommandText = "SELECT last_insert_rowid()";

            // ���������� ID ����� ���������
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// ���������� ����� ���������� � ���� ������
        /// </summary>
        /// <param name="transaction">������ ���������� ��� ����������</param>
        /// <returns>ID ����������� ����������</returns>
        public int AddTransaction(Transaction transaction)
        {
            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� ������� ����� ����������
            cmd.CommandText = @"
            INSERT INTO Transactions (Amount, Date, CategoryId, Description) 
            VALUES ($amount, $date, $categoryId, $description)";

            // ��������� ��������� � ������� ��� ������ �� SQL-��������
            cmd.Parameters.AddWithValue("$amount", transaction.Amount);
            cmd.Parameters.AddWithValue("$date", transaction.Date.ToString("o"));  // ISO 8601 format
            cmd.Parameters.AddWithValue("$categoryId", transaction.CategoryId);
            cmd.Parameters.AddWithValue("$description", transaction.Description ?? "");

            // ��������� ������ (��������� ����� ������)
            cmd.ExecuteNonQuery();

            // �������� ID ��������� ����������� ������
            cmd.CommandText = "SELECT last_insert_rowid()";

            // ���������� ID ����� ����������
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// ��������� ���� ��������� �� ���� ������
        /// </summary>
        /// <returns>������ ���� ���������</returns>
        public List<Category> GetAllCategories()
        {
            // ������� ������ ������ ��� �������� �����������
            var categories = new List<Category>();

            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� ��������� ���� ���������
            cmd.CommandText = "SELECT Id, Name FROM Categories";

            // ��������� ������ � �������� reader ��� ������ �����������
            using var reader = cmd.ExecuteReader();

            // ������ ���������� ���������
            while (reader.Read())
            {
                // ��� ������ ������ ������� ������ Category � ��������� � ������
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),        // ������ �������� ������� ������� (Id)
                    Name = reader.GetString(1)      // ������ �������� ������� ������� (Name)
                });
            }

            // ���������� ������ ���� ���������
            return categories;
        }

        /// <summary>
        /// ��������� ��������� �� ID
        /// </summary>
        /// <param name="id">ID ��������� ��� ������</param>
        /// <returns>��������� ��������� ��� null, ���� ��������� �� �������</returns>
        public Category? GetCategoryById(int id)
        {
            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // SQL-������ ��� ������ ��������� �� ID
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Id = $id";

            // ��������� �������� � ������� ��� ������ �� SQL-��������
            cmd.Parameters.AddWithValue("$id", id);

            // ��������� ������ � �������� reader ��� ������ �����������
            using var reader = cmd.ExecuteReader();

            // ���� ���� ����������, ������� � ���������� ������ Category
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),        // ������ �������� ������� ������� (Id)
                    Name = reader.GetString(1)      // ������ �������� ������� ������� (Name)
                };
            }

            // ���� ��������� �� �������, ���������� null
            return null;
        }

        /// <summary>
        /// ��������� ���������� �� ��������� ������
        /// </summary>
        /// <param name="startDate">��������� ���� ������� (������������)</param>
        /// <param name="endDate">�������� ���� ������� (������������)</param>
        /// <returns>������ ���������� �� ��������� ������</returns>
        public List<Transaction> GetTransactions(DateTime? startDate = null, DateTime? endDate = null)
        {
            // ������� ������ ������ ��� �������� �����������
            var transactions = new List<Transaction>();

            // ������� ������� ��� ���������� SQL-�������
            using var cmd = _connection.CreateCommand();

            // ������� SQL-������ ��� ��������� ����������
            cmd.CommandText = @"
            SELECT t.Id, t.Amount, t.Date, t.CategoryId, t.Description 
            FROM Transactions t
            WHERE 1=1";  // WHERE 1=1 ��������� ����� ��������� ������� ����� AND

            // ��������� ������� �� ��������� ����, ���� ��� �������
            if (startDate.HasValue)
            {
                cmd.CommandText += " AND Date >= $startDate";
                cmd.Parameters.AddWithValue("$startDate", startDate.Value.ToString("o"));
            }

            // ��������� ������� �� �������� ����, ���� ��� �������
            if (endDate.HasValue)
            {
                cmd.CommandText += " AND Date <= $endDate";
                cmd.Parameters.AddWithValue("$endDate", endDate.Value.ToString("o"));
            }

            // ��������� ������ � �������� reader ��� ������ �����������
            using var reader = cmd.ExecuteReader();

            // ������ ���������� ���������
            while (reader.Read())
            {
                // ��� ������ ������ ������� ������ Transaction � ��������� � ������
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),                    // ������ �������� ������� ������� (Id)
                    Amount = reader.GetDecimal(1),              // ������ �������� ������� ������� (Amount)
                    Date = DateTime.Parse(reader.GetString(2)), // ������ � ������ ����
                    CategoryId = reader.GetInt32(3),            // ������ �������� ���������� ������� (CategoryId)
                    Description = reader.GetString(4)           // ������ �������� ������ ������� (Description)
                });
            }

            // ���������� ������ ����������
            return transactions;
        }

        /// <summary>
        /// ������������ ��������, ��������� � ����������� � ����� ������
        /// ���������� ������������� ��� ������������� using ��� ������� ��� �������������
        /// </summary>
        public void Dispose()
        {
            // ��������� ���������� � ����� ������, ���� ��� �������
            _connection?.Close();

            // ����������� �������, ��������� � �����������
            _connection?.Dispose();
        }
    }
}