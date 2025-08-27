using System.Globalization;
using System.IO;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly string _dbPath;

        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler, string dbPath)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
            _dbPath = dbPath;
        }

        public async Task HandleCommand(long chatId, string command)
        {
            switch (command.ToLower())
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    break;

                case "/addcategory":
                    await _pendingActionHandler.ShowCategoryTypeSelection(chatId);
                    break;

                case "/report":
                    await GenerateReport(chatId);
                    break;

                case "/help":
                    await ShowHelp(chatId);
                    break;

                case "/categories":
                    await ShowExpenseCategories(chatId);
                    break;

                case "/incategories":
                    await ShowIncomeCategories(chatId);
                    break;

                case "/backup":
                    await SendDatabaseBackup(chatId);
                    break;

                case "/restore":
                    await RequestDatabaseRestore(chatId);
                    break;

                default:
                    await _bot.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help для списка команд.");
                    break;
            }
        }

        private async Task ShowExpenseCategories(long chatId)
        {
            var expenseCategories = _budgetService.GetCategoriesByType(TransactionType.Expense);

            string messegetext = "Категории РАСХОДОВ:\n" +
                $"{string.Join("\n", expenseCategories.Select(c => c.Name))}" +
                $"\n Чтобы посмотреть категроии ДОХОДОВ выполните команду: \n /incategories";

            await _bot.SendTextMessageAsync(chatId, messegetext, parseMode: ParseMode.Html);
        }

        private async Task ShowIncomeCategories(long chatId)
        {
            var incomeCategories = _budgetService.GetCategoriesByType(TransactionType.Income);

            string messegetext = "Категории ДОХОДОВ:\n" +
                $"{string.Join("\n", incomeCategories.Select(c => c.Name))}";

            await _bot.SendTextMessageAsync(chatId, messegetext, parseMode: ParseMode.Html);
        }

        private async Task ShowMainMenu(long chatId)
        {
            var menu = @"📊 <b>Управление семейным бюджетом</b>

Доступные команды:
/addcategory - Добавить категорию
/report - Показать отчет
/help - Показать справку
/categories - Показать существующие категории
/backup - Скачать резервную копию базы данных
/restore - Восстановить базу данных из резервной копии

📝 <b>Добавление транзакций:</b>
Отправьте сообщение в формате:
<code>+1500 зарплата</code> - доход
<code>-1500 продукты</code> - расход
<code>1500 продукты</code> - расход по умолчанию

<b>Доступные категории:</b>";

            await _bot.SendTextMessageAsync(chatId, menu, parseMode: ParseMode.Html);
        }

        private async Task ShowHelp(long chatId)
        {
            var helpText = @"🤖 <b>Справка по боту</b>

<b>Команды:</b>
/start - Главное меню
/addcategory - Добавить новую категорию
/report - Показать отчет за последний месяц
/backup - Скачать резервную копию базы данных
/restore - Восстановить базу данных из резервной копии
/help - Эта справка

<b>Добавление транзакций:</b>
Отправьте сообщение в формате:
<code>+1500 зарплата аванс</code> - для доходов
<code>-1500 продукты покупки</code> - для расходов
<code>1500 продукты</code> - по умолчанию расход

<b>Типы категорий:</b>
/expense - Категория расходов (траты)
/income - Категория доходов (поступления)

❗ <b>Важно:</b> Категория должна быть создана заранее с помощью команды /addcategory";

            await _bot.SendTextMessageAsync(chatId, helpText, parseMode: ParseMode.Html);
        }

        private async Task GenerateReport(long chatId)
        {
            var transactions = _budgetService.GetTransactions(
                DateTime.Now.AddMonths(-1),
                DateTime.Now
            );

            if (transactions.Count == 0)
            {
                await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за последний месяц");
                return;
            }

            var categories = _budgetService.GetAllCategories();

            var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income);
            var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense);

            var incomeReport = incomeTransactions
                .GroupBy(t => t.CategoryId)
                .Select(g => {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new { Category = category, Total = g.Sum(t => t.Amount) };
                })
                .OrderByDescending(r => r.Total);

            var expenseReport = expenseTransactions
                .GroupBy(t => t.CategoryId)
                .Select(g => {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new { Category = category, Total = g.Sum(t => t.Amount) };
                })
                .OrderByDescending(r => r.Total);

            var totalIncome = incomeReport.Sum(r => r.Total);
            var totalExpense = expenseReport.Sum(r => r.Total);

            var balance = totalIncome - totalExpense;

            var message = $"📈 <b>Отчет за последний месяц</b>\n\n" +
                          $"💰 <b>Доходы:</b> {totalIncome:C}\n" +
                          $"💸 <b>Расходы:</b> {totalExpense:C}\n" +
                          $"📊 <b>Баланс:</b> {balance:C}\n\n";

            if (incomeReport.Any())
            {
                message += "<b>Доходы по категориям:</b>\n" +
                           string.Join("\n", incomeReport.Select(r => $"- {r.Category}: {r.Total:C}")) + "\n\n";
            }

            if (expenseReport.Any())
            {
                message += "<b>Расходы по категориям:</b>\n" +
                           string.Join("\n", expenseReport.Select(r => $"- {r.Category}: {r.Total:C}")) + "\n\n";
            }

            await _bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html);
        }

        private async Task SendDatabaseBackup(long chatId)
        {
            try
            {
                // Закрываем все соединения с БД
                _budgetService.CloseConnection();

                // Даем время на освобождение файла
                await Task.Delay(1000);

                if (!System.IO.File.Exists(_dbPath))
                {
                    await _bot.SendTextMessageAsync(chatId, "❌ Файл базы данных не найден");
                    return;
                }

                // Создаем временную копию файла
                var tempBackupPath = Path.GetTempFileName();
                System.IO.File.Copy(_dbPath, tempBackupPath, true);

                await using (var stream = System.IO.File.OpenRead(tempBackupPath))
                {
                    await _bot.SendDocumentAsync(
                        chatId: chatId,
                        document: new InputOnlineFile(stream, "budget_backup.db"),
                        caption: "💾 Резервная копия базы данных"
                    );
                }

                // Удаляем временный файл
                System.IO.File.Delete(tempBackupPath);
            }
            catch (Exception ex)
            {
                await _bot.SendTextMessageAsync(chatId, $"❌ Ошибка при создании бэкапа: {ex.Message}");
            }
        }

        private async Task RequestDatabaseRestore(long chatId)
        {
            await _bot.SendTextMessageAsync(chatId,
                "📤 Отправьте файл базы данных для восстановления. Внимание: это перезапишет текущую базу данных!");

            _pendingActionHandler.SetPendingAction(chatId, "WAITING_RESTORE_FILE", null);
        }

        public async Task HandleDatabaseRestore(long chatId, Document document)
        {
            try
            {
                var file = await _bot.GetFileAsync(document.FileId);

                // Создаем временную копию текущей БД на случай ошибки
                var tempBackupPath = _dbPath + ".backup";
                if (System.IO.File.Exists(_dbPath))
                {
                    System.IO.File.Copy(_dbPath, tempBackupPath, true);
                }

                // Скачиваем и сохраняем новую БД
                await using (var saveStream = System.IO.File.OpenWrite(_dbPath))
                {
                    await _bot.DownloadFileAsync(file.FilePath, saveStream);
                }

                await _bot.SendTextMessageAsync(chatId,
                    "✅ База данных успешно восстановлена! Бот будет перезапущен.");

                // Перезапускаем приложение
                await Task.Delay(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Восстанавливаем из бекапа в случае ошибки
                if (System.IO.File.Exists(_dbPath + ".backup"))
                {
                    System.IO.File.Copy(_dbPath + ".backup", _dbPath, true);
                    System.IO.File.Delete(_dbPath + ".backup");
                }

                await _bot.SendTextMessageAsync(chatId,
                    $"❌ Ошибка при восстановлении базы данных: {ex.Message}");
            }
            finally
            {
                _pendingActionHandler.RemovePendingAction(chatId);
            }
        }
    }
}