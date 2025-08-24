using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Data.Models;
using System.Globalization;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;

        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
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

                default:
                    await _bot.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help для списка команд.");
                    break;
            }
        }

        private async Task ShowMainMenu(long chatId)
        {
            var expenseCategories = _budgetService.GetCategoriesByType(TransactionType.Expense);
            var incomeCategories = _budgetService.GetCategoriesByType(TransactionType.Income);
            var savingCategories = _budgetService.GetCategoriesByType(TransactionType.Saving);

            var menu = @"📊 <b>Управление семейным бюджетом</b>

Доступные команды:
/addcategory - Добавить категорию
/report - Показать отчет
/help - Показать справку

📝 <b>Добавление транзакций:</b>
Отправьте сообщение в формате:
<code>+1500 зарплата</code> - доход
<code>-1500 продукты</code> - расход
<code>1500 продукты</code> - расход по умолчанию

<b>Доступные категории:</b>";

            if (incomeCategories.Count != 0)
            {
                menu += $"\n💰 <b>Доходы:</b> {string.Join(", ", incomeCategories.Select(c => c.Name))}";
            }

            if (expenseCategories.Count != 0)
            {
                menu += $"\n💸 <b>Расходы:</b> {string.Join(", ", expenseCategories.Select(c => c.Name))}";
            }

            if (savingCategories.Count != 0)
            {
                menu += $"\n🏦 <b>Накопления:</b> {string.Join(", ", savingCategories.Select(c => c.Name))}";
            }

            if (!incomeCategories.Any() && !expenseCategories.Any() && !savingCategories.Any())
            {
                menu += "\nКатегории пока не добавлены";
            }

            menu += "\n\n❗ <b>Важно:</b> Категория должна быть создана заранее с помощью команды /addcategory";

            await _bot.SendTextMessageAsync(chatId, menu, parseMode: ParseMode.Html);
        }

        private async Task ShowHelp(long chatId)
        {
            var helpText = @"🤖 <b>Справка по боту</b>

<b>Команды:</b>
/start - Главное меню
/addcategory - Добавить новую категорию
/report - Показать отчет за последний месяц
/help - Эта справка

<b>Добавление транзакций:</b>
Отправьте сообщение в формате:
<code>+1500 зарплата аванс</code> - для доходов
<code>-1500 продукты покупки</code> - для расходов
<code>1500 продукты</code> - по умолчанию расход

<b>Типы категорий:</b>
/expense - Категория расходов (траты)
/income - Категория доходов (поступления)
/saving - Категория накоплений (сбережения)

❗ <b>Важно:</b> Категория должна быть создана заранее с помощью команды /addcategory";

            await _bot.SendTextMessageAsync(chatId, helpText, parseMode: ParseMode.Html);
        }

        private async Task GenerateReport(long chatId)
        {
            var transactions = _budgetService.GetTransactions(
                DateTime.Now.AddMonths(-1),
                DateTime.Now
            );

            if (!transactions.Any())
            {
                await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за последний месяц");
                return;
            }

            var categories = _budgetService.GetAllCategories();

            var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income);
            var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense);
            var savingTransactions = transactions.Where(t => t.Type == TransactionType.Saving);

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

            var savingReport = savingTransactions
                .GroupBy(t => t.CategoryId)
                .Select(g => {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new { Category = category, Total = g.Sum(t => t.Amount) };
                })
                .OrderByDescending(r => r.Total);

            var totalIncome = incomeReport.Sum(r => r.Total);
            var totalExpense = expenseReport.Sum(r => r.Total);
            var totalSaving = savingReport.Sum(r => r.Total);
            var balance = totalIncome - totalExpense - totalSaving;

            var message = $"📈 <b>Отчет за последний месяц</b>\n\n" +
                          $"💰 <b>Доходы:</b> {totalIncome:C}\n" +
                          $"💸 <b>Расходы:</b> {totalExpense:C}\n" +
                          $"🏦 <b>Накопления:</b> {totalSaving:C}\n" +
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

            if (savingReport.Any())
            {
                message += "<b>Накопления по категориям:</b>\n" +
                           string.Join("\n", savingReport.Select(r => $"- {r.Category}: {r.Total:C}"));
            }

            await _bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html);
        }
    }
}