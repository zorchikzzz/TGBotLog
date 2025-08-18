using Microsoft.Data.Sqlite;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// Модели данных
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; } = "";
}

// Репозиторий для работы с SQLite
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
        return reader.Read() ? new Category
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1)
        } : null;
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

    public void Dispose() => _connection.Close();
}

// Обработчик Telegram бота
public class BudgetBot
{
    private readonly ITelegramBotClient _bot;
    private readonly BudgetRepository _repository;
    private readonly Dictionary<long, (string Action, int? CategoryId)> _pendingActions = new();

    public BudgetBot(string token, BudgetRepository repository)
    {
        _bot = new TelegramBotClient(token);
        _repository = repository;
    }

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions
        );

        Console.WriteLine("Бот запущен...");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text!;

        try
        {
            // Проверяем ожидаемые действия в первую очередь
            if (_pendingActions.TryGetValue(chatId, out var pending))
            {
                await HandlePendingAction(chatId, text, pending);
                return;
            }

            // Обработка основных команд
            switch (text.ToLower())
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    break;

                case "/addcategory":
                    await bot.SendTextMessageAsync(chatId, "Введите название категории:");
                    _pendingActions[chatId] = ("ADD_CATEGORY", null);
                    break;

                case "/addtransaction":
                    await ShowCategorySelection(chatId);
                    break;

                case "/report":
                    await GenerateReport(chatId);
                    break;

                default:
                    await bot.SendTextMessageAsync(chatId, "Неизвестная команда");
                    break;
            }
        }
        catch (Exception ex)
        {
            await bot.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
        }
    }

    private async Task HandlePendingAction(long chatId, string text, (string Action, int? CategoryId) pending)
    {
        switch (pending.Action)
        {
            case "ADD_CATEGORY":
                _repository.AddCategory(text);
                _pendingActions.Remove(chatId);
                await _bot.SendTextMessageAsync(chatId, $"✅ Категория '{text}' добавлена!");
                break;

            case "SELECT_CATEGORY":
                if (int.TryParse(text, out int categoryId))
                {
                    var category = _repository.GetCategoryById(categoryId);
                    if (category != null)
                    {
                        await _bot.SendTextMessageAsync(
                            chatId,
                            $"📝 Выбрана категория: {category.Name}\nВведите сумму и описание через пробел:"
                        );
                        _pendingActions[chatId] = ("ADD_TRANSACTION", categoryId);
                    }
                    else
                    {
                        await _bot.SendTextMessageAsync(chatId, "❌ Категория не найдена");
                        await ShowCategorySelection(chatId);
                    }
                }
                else
                {
                    await _bot.SendTextMessageAsync(chatId, "❌ Неверный формат. Введите номер категории:");
                    await ShowCategorySelection(chatId);
                }
                break;

            case "ADD_TRANSACTION":
                var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1 && decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    var description = parts.Length > 1 ? parts[1] : "Без описания";

                    var transaction = new Transaction
                    {
                        Amount = amount,
                        Date = DateTime.Now,
                        CategoryId = pending.CategoryId ?? 0,
                        Description = description
                    };

                    _repository.AddTransaction(transaction);
                    _pendingActions.Remove(chatId);
                    await _bot.SendTextMessageAsync(chatId, "✅ Транзакция добавлена!");
                }
                else
                {
                    await _bot.SendTextMessageAsync(chatId, "❌ Неверный формат. Пример: 1500 Покупка продуктов");
                    await _bot.SendTextMessageAsync(
                        chatId,
                        $"Введите сумму и описание через пробел:"
                    );
                }
                break;
        }
    }

    private async Task ShowMainMenu(long chatId)
    {
        var menu = @"📊 <b>Управление семейным бюджетом</b>

Доступные команды:
/addcategory - Добавить категорию
/addtransaction - Добавить транзакцию
/report - Показать отчет за последний месяц";

        await _bot.SendTextMessageAsync(
            chatId,
            menu,
            parseMode: ParseMode.Html
        );
    }

    private async Task ShowCategorySelection(long chatId)
    {
        var categories = _repository.GetAllCategories();
        if (!categories.Any())
        {
            await _bot.SendTextMessageAsync(chatId, "ℹ️ Сначала добавьте категории через /addcategory");
            return;
        }

        var message = "Выберите категорию:\n" +
                      string.Join("\n", categories.Select(c => $"{c.Id}. {c.Name}")) +
                      "\n\nВведите номер категории:";

        await _bot.SendTextMessageAsync(chatId, message);
        _pendingActions[chatId] = ("SELECT_CATEGORY", null);
    }

    private async Task GenerateReport(long chatId)
    {
        var transactions = _repository.GetTransactions(
            DateTime.Now.AddMonths(-1),
            DateTime.Now
        );

        if (!transactions.Any())
        {
            await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за последний месяц");
            return;
        }

        var categories = _repository.GetAllCategories();
        var report = transactions
            .GroupBy(t => t.CategoryId)
            .Select(g => {
                var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                return new
                {
                    Category = category,
                    Total = g.Sum(t => t.Amount)
                };
            })
            .OrderByDescending(r => r.Total);

        var totalExpenses = report.Sum(r => r.Total);
        var message = $"📈 <b>Отчет за последний месяц</b>\nОбщие расходы: {totalExpenses:C}\n\n" +
                      "<b>По категориям:</b>\n" +
                      string.Join("\n", report.Select(r => $"- {r.Category}: {r.Total:C}"));

        await _bot.SendTextMessageAsync(
            chatId,
            message,
            parseMode: ParseMode.Html
        );
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}

// Точка входа
class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var repository = new BudgetRepository();
        var bot = new BudgetBot("5829830933:AAFm29KTHLtOFoF4YM5_Kq_GN2OEhHKR_oU", repository);
        bot.Start();

        Console.WriteLine("Приложение для управления бюджетом запущено...");
        Console.WriteLine("Нажмите Enter для выхода");
        Console.ReadLine();
    }
}