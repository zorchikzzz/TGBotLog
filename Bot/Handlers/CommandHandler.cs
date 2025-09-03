using System.Globalization;
using System.IO;
using System.Text;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TGBotLog.Bot.Services;
using TGBotLog.Data.Models;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly BackupService _backupHandler;
        


        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler, BackupService backupHandler, string dbPath)
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

                case "/incategories": 
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

            await _bot.SendTextMessageAsync(chatId, messegetext, parseMode: ParseMode.Html, replyMarkup: Keyboards.showIncomeCategoriesButton);
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
            await _bot.SendTextMessageAsync(chatId, "ГЛАВНОЕ МЕНЮ", parseMode: ParseMode.Html, replyMarkup: Keyboards.MainMenu);
            await _bot.SendTextMessageAsync(chatId, MessegeTexts.MenuText, parseMode: ParseMode.Html);
        }

       
        public async Task ShowHelp(long chatId)
        {
            await _bot.SendTextMessageAsync(chatId, MessegeTexts.HelpText, parseMode: ParseMode.Html);
        }

        public async Task GenerateReport(long chatId)
        {
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
           
            var transactions = _budgetService.GetTransactions(
                firstDayOfMonth,
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
                          $"💰 <b>Доходы:</b>   {totalIncome:N0}\n" +
                          $"💸 <b>Расходы:</b>  {totalExpense:N0}\n" +
                          $"📊 <b>Баланс:</b>   {balance:N0}\n\n";

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


        public async Task ShowLast10Transactions(long chatId)
        {
            var transactions = _budgetService.GetTransactions(null, null, true);

            if (transactions == null || transactions.Count == 0)
            {
                await _bot.SendTextMessageAsync(chatId, "Нет данных о транзакциях");
                return;
            }

            var categories = _budgetService.GetAllCategories();

            // Создаем таблицу с выравниванием
            var message = new StringBuilder();
            message.AppendLine("💳 <b>ПОСЛЕДНИЕ 10 ОПЕРАЦИЙ:</b>\n");

            // Добавляем заголовок таблицы
            message.AppendLine("<pre>");
            message.AppendLine("Тип        Сумма   Категория        Дата и время");
            message.AppendLine("------------------------------------------------");

            // Добавляем строки с транзакциями
            foreach (var transaction in transactions)
            {
                var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                var categoryName = category?.Name ?? "Неизвестная";

                // Обрезаем длинные названия категорий
                if (categoryName.Length > 15)
                    categoryName = categoryName.Substring(0, 12) + "...";

                // Форматируем дату
                var date = transaction.Date.ToString("ddd dd.MM. HH:mm");

                // Форматируем сумму с выравниванием
                var amount = transaction.Amount.ToString("N0").PadLeft(8);
                var typeSign = transaction.Type == TransactionType.Income ? "ДОХОД" : "РАСХОД";

                // Добавляем строку в таблицу
                message.AppendLine(
            $"{typeSign,-7} " +    // Тип операции (7 символов, выравнивание по левому краю)
            $"{amount,8}   " +       // Сумма (8 символов, выравнивание по правому краю)
            $"{categoryName,-13} " + // Категория (15 символов, выравнивание по левому краю)
            $"{date}"              // Дата
        );

            }

            message.AppendLine("</pre>");

            await _bot.SendTextMessageAsync(chatId, message.ToString(), parseMode: ParseMode.Html);
        }

    }
}