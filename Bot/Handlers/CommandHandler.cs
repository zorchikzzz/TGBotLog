using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly BackupHandler _backupHandler;


        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler, BackupHandler backupHandler, string dbPath)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
            _backupHandler = backupHandler;


        }

        public async Task HandleCommand(long chatId, string command)
        {

            if (command.Contains('@'))
            {
                command = command.Split('@')[0];
            }

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

                case "/incategories": //
                    await ShowIncomeCategories(chatId);
                    break;

                case "/backup":
                    await _backupHandler.SendDatabaseBackup(chatId);
                    break;

                case "/restore":
                    await _backupHandler.RequestDatabaseRestore(chatId);
                    break;

                default:
                    await _bot.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте команду /help для справки.");
                    break;
            }
        }

        public async Task ShowExpenseCategories(long chatId)
        {
            var expenseCategories = _budgetService.GetCategoriesByType(TransactionType.Expense);

            string messegetext = @"<b>КАТЕГОРИИ РАСХОДОВ:</b>" + "\n" +
                $"{string.Join("\n", expenseCategories.Select(c => c.Name))}";

            var keyboard = new InlineKeyboardMarkup(new[]
                      {
                 new[]
                 {
                     InlineKeyboardButton.WithCallbackData("КАТЕГОРИИ ДОХОДОВ", "income_categories"),
                 }
             });

            await _bot.SendTextMessageAsync(chatId, messegetext, parseMode: ParseMode.Html, replyMarkup: keyboard);
        }

        public async Task ShowIncomeCategories(long chatId)
        {
            var incomeCategories = _budgetService.GetCategoriesByType(TransactionType.Income);

            string messegetext = @"<b>КАТЕГОРИИ ДОХОДОВ:</b>"+ "\n" +
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
<code>+1500 зарплата</code> - доход";

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "ОТЧЁТ" , "КАТЕГОРИИ" },
                new KeyboardButton[] { "СПРАВКА" , "ДОБАВИТЬ КАТЕГОРИЮ" },


            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _bot.SendTextMessageAsync(chatId, "ГЛАВНОЕ МЕНЮ", parseMode: ParseMode.Html, replyMarkup: keyboard);
            await _bot.SendTextMessageAsync(chatId, menu, parseMode: ParseMode.Html);

        }

        public async Task ShowHelp(long chatId)
        {
            var helpText = @"🤖 <b>Справка по боту</b>

<b>Команды которые можно выполнить:</b>
/start - Главное меню
/addcategory - Добавить новую категорию
/report - Показать отчет за последний месяц
/backup - Скачать резервную копию базы данных
/restore - Восстановить базу данных из резервной копии
/help - Это информация о боте.

<b>Добавление транзакций:</b>
Отправьте сообщение в формате:
<code>+1500(сумма) ПРОДУКТЫ (категория кторая должна быть добавлена заранее) КОММЕНТАРИЙ (опционально, можо доплнить транзакцию дополнительными сведениями котрые внесут ясность вдальнейшем)</code> 


<b>Типы категорий:</b>
/expense - Категория расходов (траты)
/income - Категория доходов (поступления)

❗ <b>Важно:</b> Категория должна быть создана заранее с помощью команды
/addcategory";

            await _bot.SendTextMessageAsync(chatId, helpText, parseMode: ParseMode.Html);
        }

        public async Task GenerateReport(long chatId)
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
                .Select(g =>
                {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new { Category = category, Total = g.Sum(t => t.Amount) };
                })
                .OrderByDescending(r => r.Total);

            var expenseReport = expenseTransactions
                .GroupBy(t => t.CategoryId)
                .Select(g =>
                {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new { Category = category, Total = g.Sum(t => t.Amount) };
                })
                .OrderByDescending(r => r.Total);

            var totalIncome = incomeReport.Sum(r => r.Total);
            var totalExpense = expenseReport.Sum(r => r.Total);

            var balance = totalIncome - totalExpense;

            var message = $"📈 <b>Отчет за последний месяц</b>\n\n" +
                          $"💰 <b>Доходы:</b> {totalIncome:N0}\n" +
                          $"💸 <b>Расходы:</b> {totalExpense:N0}\n" +
                          $"📊 <b>Баланс:</b> {balance:N0}\n\n";

            if (incomeReport.Any())
            {
                message += "<b>Доходы по категориям:</b>\n" +
                           string.Join("\n", incomeReport.Select(r => $"- {r.Category}: {r.Total:N0} ")) + "\n\n";
            }

            if (expenseReport.Any())
            {
                message += "<b>Расходы по категориям:</b>\n" +
                           string.Join("\n", expenseReport.Select(r => $"- {r.Category}: {r.Total:N0} ")) + "\n\n";
            }

            await _bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html);
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var data = callbackQuery.Data;

            // Ответ на callback query (убирает "часик" loading)
            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);

            // Обработка различных callback данных
            switch (data)
            {
                case "income_categories":
                    // Обновляем сообщение с новой клавиатурой
                    await ShowIncomeCategories(chatId);
                    break;

                case "btn2":
                    // Отправляем новое сообщение
                    await _bot.SendTextMessageAsync(chatId, "Вы нажали кнопку 2");
                    break;

                    // Добавьте другие case для обработки различных callback данных
            }

        }
    }
}