using Microsoft.Data.Sqlite;
using FamilyBudgetBot.Data.Models;
using System.Globalization;

namespace FamilyBudgetBot.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с базой данных SQLite
    /// Обеспечивает все операции с данными: создание, чтение, обновление, удаление
    /// Реализует IDisposable для правильного освобождения ресурсов
    /// </summary>
    public class BudgetRepository : IDisposable
    {
        // Соединение с базой данных SQLite
        private readonly SqliteConnection _connection;

        /// <summary>
        /// Конструктор репозитория
        /// </summary>
        /// <param name="dbPath">Путь к файлу базы данных (по умолчанию "budget.db")</param>
        public BudgetRepository(string dbPath = "budget.db")
        {
            // Создаем соединение с SQLite базой данных
            // Data Source указывает на файл, где хранятся данные
            _connection = new SqliteConnection($"Data Source={dbPath}");

            // Открываем соединение с базой данных
            _connection.Open();

            // Инициализируем структуру базы данных (создаем таблицы, если они не существуют)
            InitializeDatabase();
        }

        /// <summary>
        /// Инициализация структуры базы данных
        /// Создает необходимые таблицы, если они не существуют
        /// </summary>
        private void InitializeDatabase()
        {
            // Используем команду для выполнения SQL-запросов
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для создания таблицы категорий, если она не существует
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,    -- Автоинкрементируемый первичный ключ
                Name TEXT NOT NULL UNIQUE               -- Уникальное название категории
            )";
            cmd.ExecuteNonQuery();  // Выполняем запрос

            // SQL-запрос для создания таблицы транзакций, если она не существует
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,    -- Автоинкрементируемый первичный ключ
                Amount REAL NOT NULL,                   -- Сумма транзакции
                Date TEXT NOT NULL,                     -- Дата транзакции в текстовом формате
                CategoryId INTEGER NOT NULL,            -- Внешний ключ к таблице Categories
                Description TEXT,                       -- Описание транзакции (может быть NULL)
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)  -- Связь с таблицей Categories
            )";
            cmd.ExecuteNonQuery();  // Выполняем запрос
        }

        /// <summary>
        /// Поиск категории по имени
        /// </summary>
        /// <param name="name">Название категории для поиска</param>
        /// <returns>Найденная категория или null, если категория не найдена</returns>
        public Category? GetCategoryByName(string name)
        {
            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для поиска категории по имени
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Name = $name";

            // Добавляем параметр к запросу для защиты от SQL-инъекций
            cmd.Parameters.AddWithValue("$name", name);

            // Выполняем запрос и получаем reader для чтения результатов
            using var reader = cmd.ExecuteReader();

            // Если есть результаты, создаем и возвращаем объект Category
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),        // Читаем значение первого столбца (Id)
                    Name = reader.GetString(1)      // Читаем значение второго столбца (Name)
                };
            }

            // Если категория не найдена, возвращаем null
            return null;
        }

        /// <summary>
        /// Добавление новой категории в базу данных
        /// </summary>
        /// <param name="name">Название новой категории</param>
        /// <returns>ID добавленной категории</returns>
        public int AddCategory(string name)
        {
            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для вставки новой категории
            cmd.CommandText = "INSERT INTO Categories (Name) VALUES ($name)";

            // Добавляем параметр к запросу для защиты от SQL-инъекций
            cmd.Parameters.AddWithValue("$name", name);

            // Выполняем запрос (вставляем новую запись)
            cmd.ExecuteNonQuery();

            // Получаем ID последней добавленной записи
            cmd.CommandText = "SELECT last_insert_rowid()";

            // Возвращаем ID новой категории
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Добавление новой транзакции в базу данных
        /// </summary>
        /// <param name="transaction">Объект транзакции для добавления</param>
        /// <returns>ID добавленной транзакции</returns>
        public int AddTransaction(Transaction transaction)
        {
            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для вставки новой транзакции
            cmd.CommandText = @"
            INSERT INTO Transactions (Amount, Date, CategoryId, Description) 
            VALUES ($amount, $date, $categoryId, $description)";

            // Добавляем параметры к запросу для защиты от SQL-инъекций
            cmd.Parameters.AddWithValue("$amount", transaction.Amount);
            cmd.Parameters.AddWithValue("$date", transaction.Date.ToString("o"));  // ISO 8601 format
            cmd.Parameters.AddWithValue("$categoryId", transaction.CategoryId);
            cmd.Parameters.AddWithValue("$description", transaction.Description ?? "");

            // Выполняем запрос (вставляем новую запись)
            cmd.ExecuteNonQuery();

            // Получаем ID последней добавленной записи
            cmd.CommandText = "SELECT last_insert_rowid()";

            // Возвращаем ID новой транзакции
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Получение всех категорий из базы данных
        /// </summary>
        /// <returns>Список всех категорий</returns>
        public List<Category> GetAllCategories()
        {
            // Создаем пустой список для хранения результатов
            var categories = new List<Category>();

            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для получения всех категорий
            cmd.CommandText = "SELECT Id, Name FROM Categories";

            // Выполняем запрос и получаем reader для чтения результатов
            using var reader = cmd.ExecuteReader();

            // Читаем результаты построчно
            while (reader.Read())
            {
                // Для каждой строки создаем объект Category и добавляем в список
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),        // Читаем значение первого столбца (Id)
                    Name = reader.GetString(1)      // Читаем значение второго столбца (Name)
                });
            }

            // Возвращаем список всех категорий
            return categories;
        }

        /// <summary>
        /// Получение категории по ID
        /// </summary>
        /// <param name="id">ID категории для поиска</param>
        /// <returns>Найденная категория или null, если категория не найдена</returns>
        public Category? GetCategoryById(int id)
        {
            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // SQL-запрос для поиска категории по ID
            cmd.CommandText = "SELECT Id, Name FROM Categories WHERE Id = $id";

            // Добавляем параметр к запросу для защиты от SQL-инъекций
            cmd.Parameters.AddWithValue("$id", id);

            // Выполняем запрос и получаем reader для чтения результатов
            using var reader = cmd.ExecuteReader();

            // Если есть результаты, создаем и возвращаем объект Category
            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetInt32(0),        // Читаем значение первого столбца (Id)
                    Name = reader.GetString(1)      // Читаем значение второго столбца (Name)
                };
            }

            // Если категория не найдена, возвращаем null
            return null;
        }

        /// <summary>
        /// Получение транзакций за указанный период
        /// </summary>
        /// <param name="startDate">Начальная дата периода (включительно)</param>
        /// <param name="endDate">Конечная дата периода (включительно)</param>
        /// <returns>Список транзакций за указанный период</returns>
        public List<Transaction> GetTransactions(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Создаем пустой список для хранения результатов
            var transactions = new List<Transaction>();

            // Создаем команду для выполнения SQL-запроса
            using var cmd = _connection.CreateCommand();

            // Базовый SQL-запрос для получения транзакций
            cmd.CommandText = @"
            SELECT t.Id, t.Amount, t.Date, t.CategoryId, t.Description 
            FROM Transactions t
            WHERE 1=1";  // WHERE 1=1 позволяет легко добавлять условия через AND

            // Добавляем условие по начальной дате, если она указана
            if (startDate.HasValue)
            {
                cmd.CommandText += " AND Date >= $startDate";
                cmd.Parameters.AddWithValue("$startDate", startDate.Value.ToString("o"));
            }

            // Добавляем условие по конечной дате, если она указана
            if (endDate.HasValue)
            {
                cmd.CommandText += " AND Date <= $endDate";
                cmd.Parameters.AddWithValue("$endDate", endDate.Value.ToString("o"));
            }

            // Выполняем запрос и получаем reader для чтения результатов
            using var reader = cmd.ExecuteReader();

            // Читаем результаты построчно
            while (reader.Read())
            {
                // Для каждой строки создаем объект Transaction и добавляем в список
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),                    // Читаем значение первого столбца (Id)
                    Amount = reader.GetDecimal(1),              // Читаем значение второго столбца (Amount)
                    Date = DateTime.Parse(reader.GetString(2)), // Читаем и парсим дату
                    CategoryId = reader.GetInt32(3),            // Читаем значение четвертого столбца (CategoryId)
                    Description = reader.GetString(4)           // Читаем значение пятого столбца (Description)
                });
            }

            // Возвращаем список транзакций
            return transactions;
        }

        /// <summary>
        /// Освобождение ресурсов, связанных с соединением с базой данных
        /// Вызывается автоматически при использовании using или вручную при необходимости
        /// </summary>
        public void Dispose()
        {
            // Закрываем соединение с базой данных, если оно открыто
            _connection?.Close();

            // Освобождаем ресурсы, связанные с соединением
            _connection?.Dispose();
        }
    }
}