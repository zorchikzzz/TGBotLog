using System.Globalization;
using System.Text;
using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TGBotLog.Data.Models;
using static FamilyBudgetBot.Services.ChartService;

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

        var transactions = _budgetService.GetTransactions(startDate, endDate);
        var categories = _budgetService.GetAllCategories();
        var inlineKeyboard = CreateReportKeyboard(year, month);

        if (transactions.Count == 0)
        {
            await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за выбранный период", parseMode: ParseMode.Html,
           replyMarkup: CreateReportKeyboard(year,month, false));
            
            return;
        }

        var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income);
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense);

        var incomeReport = incomeTransactions
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var category = categories.FirstOrDefault(c => c.Id == g.Key) ?? new Category();
                return new
                {
                    CategoryId = g.Key,
                    Category = category.Name,
                    Total = g.Sum(t => t.Amount)
                };
            })
            .OrderByDescending(r => r.Total);

        var expenseReport = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var category = categories.FirstOrDefault(c => c.Id == g.Key) ?? new Category();
                return new
                {
                    CategoryId = g.Key,
                    Category = category.Name,
                    Total = g.Sum(t => t.Amount)
                };
            })
            .OrderByDescending(r => r.Total);

        var totalIncome = incomeReport.Sum(r => r.Total);
        var totalExpense = expenseReport.Sum(r => r.Total);
        var balance = totalIncome - totalExpense;

        var message = new StringBuilder();
        message.AppendLine($"📈 <b>Отчет за {periodTitle}</b>\n");
        message.AppendLine($"💰 <b>Доходы:</b> {totalIncome:N0} руб.");
        message.AppendLine($"💸 <b>Расходы:</b> {totalExpense:N0} руб.");
        message.AppendLine($"📊 <b>Баланс:</b> {balance:N0} руб.\n");

        if (incomeReport.Any())
        {
            message.AppendLine("<b>Доходы по категориям:</b>");
            foreach (var item in incomeReport)
            {
                message.AppendLine($"  {item.Total,7:N0} руб. - {item.Category}");
            }
            message.AppendLine();
        }

        if (expenseReport.Any())
        {
            message.AppendLine("<b>Расходы по категориям:</b>");
            foreach (var item in expenseReport)
            {
                message.AppendLine($"  {item.Total,7:N0} руб. - {item.Category}");
            }
            message.AppendLine();
        }

        // Создаем клавиатуру с кнопками для детализированного отчета и выбора периода

     


        await _bot.SendTextMessageAsync(
            chatId: chatId,
            text: message.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }

    public async Task GenerateDetailedReport(long chatId, int? year = null, int? month = null)
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

        var transactions = _budgetService.GetTransactions(startDate, endDate);
        var categories = _budgetService.GetAllCategories();

        if (transactions.Count == 0)
        {
            await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за выбранный период");
            return;
        }

        var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income);
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense);

        var incomeReport = incomeTransactions
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var category = categories.FirstOrDefault(c => c.Id == g.Key) ?? new Category();
                return new
                {
                    CategoryId = g.Key,
                    Category = category.Name,
                    Total = g.Sum(t => t.Amount)
                };
            })
            .OrderByDescending(r => r.Total);

        var expenseReport = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var category = categories.FirstOrDefault(c => c.Id == g.Key) ?? new Category();
                return new
                {
                    CategoryId = g.Key,
                    Category = category.Name,
                    Total = g.Sum(t => t.Amount)
                };
            })
            .OrderByDescending(r => r.Total);

        var totalIncome = incomeReport.Sum(r => r.Total);
        var totalExpense = expenseReport.Sum(r => r.Total);
        var balance = totalIncome - totalExpense;

        var message = new StringBuilder();
        message.AppendLine($"📈 <b>Отчет за {periodTitle}</b>\n");
        message.AppendLine($"💰 <b>Доходы:</b> {totalIncome:N0} руб.");
        message.AppendLine($"💸 <b>Расходы:</b> {totalExpense:N0} руб.");
        message.AppendLine($"📊 <b>Баланс:</b> {balance:N0} руб.\n");
        message.AppendLine("Нажмите на категорию для просмотра деталей:");

        // Создаем клавиатуру с кнопками категорий в два столбца и кнопкой выбора периода
        var inlineKeyboard = CreateDetailedReportKeyboard(incomeReport, expenseReport, year, month);
              
        await _bot.SendTextMessageAsync(
            chatId: chatId,
            text: message.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );

        // Обрабаотываем уже полученные данные по категриям и расходам для того что бы передать их в обработанном виде в конструктор диагарам
        var chartDataList = new List<CategoryChartData>();
        foreach (var category in categories)
        {
            if (category.Name == "ЗП")
                continue;
            var incomeTotal = incomeReport.FirstOrDefault(r => r.Category == category.Name)?.Total ?? 0;
            var expenseTotal = expenseReport.FirstOrDefault(r => r.Category == category.Name)?.Total ?? 0;
            if (incomeTotal > 0 || expenseTotal > 0)
            {
                chartDataList.Add(new CategoryChartData
                {
                    CategoryName = category.Name,
                    Income = incomeTotal,
                    Expense = expenseTotal
                });
            }
        }

        // Создаём ChartService (сервис по графикам и диагармам) (репозиторий нужен для конструктора, но не используется в этом методе)
        var chartService = new ChartService(_budgetService.GetRepository());
        var chartBytes = chartService.GenerateCategoryChart(chartDataList, periodTitle);

        if (chartBytes != null)
        {
            using var stream = new MemoryStream(chartBytes);
            await _bot.SendPhotoAsync(chatId, new InputOnlineFile(stream, "categories.png"),
                caption: "📊 График по категориям");
        }
    }

    private InlineKeyboardMarkup CreateReportKeyboard(int? year, int? month, bool hasdata = true)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Добавляем кнопку деталей только если есть данные
        if (hasdata)
        {
            var detailedCallbackData = $"detailed_report_{year ?? DateTime.Now.Year}_{month ?? DateTime.Now.Month}";
            buttons.Add(new[]
            {
            InlineKeyboardButton.WithCallbackData("📊 Показать детали по категориям", detailedCallbackData)
        });
        }

        // Всегда добавляем кнопку выбора периода
        buttons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("📅 Выбрать период", "select_report_period")
    });

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup CreateDetailedReportKeyboard(
        IEnumerable<dynamic> incomeReport,
        IEnumerable<dynamic> expenseReport,
        int? year,
        int? month)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Добавляем категории доходов в два столбца
        var incomeButtons = new List<InlineKeyboardButton>();
        foreach (var item in incomeReport)
        {
            var callbackData = $"category_details_{year ?? DateTime.Now.Year}_{month ?? DateTime.Now.Month}_{item.CategoryId}";
            incomeButtons.Add(InlineKeyboardButton.WithCallbackData(
                $"💰 {item.Category}",
                callbackData));
        }

        // Разбиваем кнопки доходов на два столбца
        for (int i = 0; i < incomeButtons.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(incomeButtons[i]);
            if (i + 1 < incomeButtons.Count)
                row.Add(incomeButtons[i + 1]);
            buttons.Add(row.ToArray());
        }

        // Добавляем разделитель между доходами и расходами
        if (incomeButtons.Any() && expenseReport.Any())
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("────────────", "no_action")
            });
        }

        // Добавляем категории расходов в два столбца
        var expenseButtons = new List<InlineKeyboardButton>();
        foreach (var item in expenseReport)
        {
            var callbackData = $"category_details_{year ?? DateTime.Now.Year}_{month ?? DateTime.Now.Month}_{item.CategoryId}";
            expenseButtons.Add(InlineKeyboardButton.WithCallbackData(
                $"💸 {item.Category}",
                callbackData));
        }

        // Разбиваем кнопки расходов на два столбца
        for (int i = 0; i < expenseButtons.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(expenseButtons[i]);
            if (i + 1 < expenseButtons.Count)
                row.Add(expenseButtons[i + 1]);
            buttons.Add(row.ToArray());
        }

        // Добавляем кнопки навигации
        var backCallbackData = $"back_to_report_{year ?? DateTime.Now.Year}_{month ?? DateTime.Now.Month}";
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("◀️ Назад к отчету", backCallbackData),
            InlineKeyboardButton.WithCallbackData("📅 Выбрать период", "select_report_period")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    public async Task ShowCategoryDetails(long chatId, int year, int month, int categoryId)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = _budgetService.GetTransactionsByCategoryAndPeriod(categoryId, startDate, endDate);
        var category = _budgetService.GetCategoryById(categoryId);
        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

        if (transactions.Count == 0)
        {
            await _bot.SendTextMessageAsync(chatId, $"Нет транзакций в категории '{category?.Name}' за {monthName} {year}");
            return;
        }

        var totalAmount = transactions.Sum(t => t.Amount);
        var message = new StringBuilder();

        message.AppendLine($"📋 <b>Детализация:</b> {category?.Name}");
        message.AppendLine($"📅 <b>Период:</b> {monthName} {year}");
        message.AppendLine($"💵 <b>Общая сумма:</b> {totalAmount:N0} руб.");
        message.AppendLine($"📊 <b>Количество транзакций:</b> {transactions.Count}\n");
        message.AppendLine("<b>Транзакции:</b>\n");

        foreach (var transaction in transactions.OrderByDescending(t => t.Date))
        {
            var date = transaction.Date.ToString("dd.MM");
            var description = string.IsNullOrEmpty(transaction.Description) ? "Без описания" : transaction.Description;
            var typeIcon = transaction.Type == TransactionType.Income ? "➕" : "➖";

            message.AppendLine($"{typeIcon} <code>{date}</code> - {transaction.Amount:N0} руб.");
            message.AppendLine($"   <i>{description}</i>\n");
        }

        // Кнопки навигации
        var backButton = InlineKeyboardButton.WithCallbackData(
            "◀️ Назад к детализации",
            $"detailed_report_{year}_{month}"
        );

        var mainReportButton = InlineKeyboardButton.WithCallbackData(
            "📊 Основной отчет",
            $"back_to_report_{year}_{month}"
        );

        var periodButton = InlineKeyboardButton.WithCallbackData(
            "📅 Выбрать период",
            "select_report_period"
        );

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { backButton, mainReportButton },
            new[] { periodButton }
        });

        await _bot.SendTextMessageAsync(
            chatId: chatId,
            text: message.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}