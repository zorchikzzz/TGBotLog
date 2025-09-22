using System.Globalization;
using System.Text;
using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TGBotLog.Data.Models;

namespace TGBotLog.Bot.Services;

public class ReportService
{
 private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
    public ReportService(BudgetService budgetService, ITelegramBotClient bot)
    {
        _budgetService = budgetService;
        _bot = bot;
    }

   public async Task GenerateReport(long chatId, int? year = null, int? month = null)
    {
        DateTime startDate, endDate;
        string periodTitle;

        if (year.HasValue && month.HasValue)
        {
            startDate = new DateTime(year.Value, month.Value, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value);
            periodTitle = $"{monthName} {year.Value}";
        }
        else
        {
            startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            endDate = DateTime.Now;
            periodTitle = "последний месяц";
        }

        var transactions = _budgetService.GetTransactions(
            startDate,
            endDate
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

        var message = new StringBuilder();
        message.AppendLine(
            $"📈 <b>Отчет за {periodTitle}</b>\n\n" +
            $"💰 <b>Доходы:</b>    {totalIncome,-7:N0}\n" +
            $"💸 <b>Расходы:</b>   {totalExpense,-7:N0}\n" +
            $"📊 <b>Баланс:</b>    {balance,7:N0}\n\n"
                            );

        if (incomeReport.Any())
        {
            message.AppendLine("<b>Доходы по категориям:</b>\n" +
                       string.Join("\n", incomeReport.Select(r => $"{r.Total,7:N0}                   {r.Category,-9}")) + "\n\n");
        }

        if (expenseReport.Any())
        {
            message.AppendLine("<b>Расходы по категориям:</b>\n" +
                       string.Join("\n", expenseReport.Select(r => $"{r.Total,7:N0}                   {r.Category,-9}")) + "\n\n");
        }


        await _bot.SendTextMessageAsync(chatId, message.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: Keyboards.SelectReportPeriod);
    }



}
