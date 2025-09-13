using System.Globalization;
using System.Text;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
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
        private readonly ReportService _reportService;



        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler, BackupService backupHandler, ReportService reportService, string dbPath)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
            _backupHandler = backupHandler;
            _reportService = reportService;

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
                    await _reportService.GenerateReport(chatId);
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
          
            // Добавляем строки с транзакциями
            foreach (var transaction in transactions)
            {
                var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                var categoryName = category?.Name ?? "Неизвестная";
                var description = transaction.Description;
                

                // Обрезаем длинные названия категорий
                if (categoryName.Length > 15)
                    categoryName = categoryName.Substring(0, 12) + "...";
                if (description.Length > 16)
                    description = description.Substring(0, 14) + "...";

                // Форматируем дату
                //var date = transaction.Date.ToString("dd.MM");

                // Форматируем сумму с выравниванием
                var amount = transaction.Amount.ToString("N0");
                var typeSign = transaction.Type == TransactionType.Income ? "➕" : "➖";

                // Добавляем строку в таблицу
                message.AppendLine(
            $"{typeSign} " +    // Тип операции (7 символов, выравнивание по левому краю)
            $"{amount,-5} " +       // Сумма (8 символов, выравнивание по правому краю)
            $" {categoryName,-10} " + // Категория (15 символов, выравнивание по левому краю)
           // $"{date,-5} " +
            $"{description,-15}"

        );

            }

            message.AppendLine("</pre>");

            await _bot.SendTextMessageAsync(chatId, message.ToString(), parseMode: ParseMode.Html);
        }

    }
}