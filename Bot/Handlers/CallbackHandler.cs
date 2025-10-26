using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using TGBotLog.Data.Models;
using TGBotLog.Bot.Services;


namespace TGBotLog.Bot.Handlers
{
    public class CallbackHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly CommandHandler _commandHandler;
        private readonly BudgetService _budgetService;
        private readonly ReportService _reportService;
        // словарь для хранения последнего выбранного года
        private readonly Dictionary<long, int> _lastSelectedYear = new Dictionary<long, int>();

        public CallbackHandler(ITelegramBotClient bot, PendingActionHandler pendingActionHandler,
            CommandHandler commandHandler, BudgetService budgetService, ReportService reportService)
        {
            _bot = bot;
            _pendingActionHandler = pendingActionHandler;
            _budgetService = budgetService;
            _commandHandler = commandHandler;
            _reportService = reportService;
            
        }


        // В CallbackHandler.cs
        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            if (callbackQuery?.Message == null) return;

            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;



            Console.WriteLine($"Получен callback: {data}");

            if (string.IsNullOrEmpty(data)) return;

            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);

            switch (data)
            {
                case "income_categories":
                    await _commandHandler.ShowIncomeCategories(chatId);
                    break;

                case "income_categorie_selected":
                    await _pendingActionHandler.HandleCategoryTypeSelection(chatId, "ДОХОД");
                    break;

                case "expance_categorie_selected":
                    await _pendingActionHandler.HandleCategoryTypeSelection(chatId, "РАСХОД");
                    break;

// Вход сюда происходит из очтёта за любой месяц (показываютсямесяца того же года или текущего(если не выбран год))
                case "select_report_period":
                    int yearToShow = _lastSelectedYear.ContainsKey(chatId)
                        ? _lastSelectedYear[chatId]
                        : DateTime.Now.Year;

                    await ShowYearMonths(chatId, yearToShow);
                    break;
               
// Вход сюда происходит из меню выбора года (показываются месяца года выбранного в меню)
                case string s when s.StartsWith("select_month_"):
                    if (int.TryParse(s.Substring("select_month_".Length), out int year))
                    {
                        // Сохраняем выбранный год
                        _lastSelectedYear[chatId] = year;
                        await ShowYearMonths(chatId, year);
                    }
                    break;

                case "select_report_year":
                    await ShowYearSelection(chatId);
                    break;

// Вход сюда происходит из меню выбора месяца, нажатием на месяц (запускает генерацию отчтёта)
                case string s when s.StartsWith("report_month_"):
                    var parts = s.Split('_');
                    if (parts.Length >= 4 && int.TryParse(parts[2], out int reportYear) &&
                        int.TryParse(parts[3], out int reportMonth))
                    {
                        // Сохраняем выбранный год
                        _lastSelectedYear[chatId] = reportYear;
                        await _reportService.GenerateReport(chatId, reportYear, reportMonth);
                    }
                    break;

                // В CallbackHandler.cs в метод HandleCallbackQuery добавить новый case
                case string s when s.StartsWith("category_details_"):
                    var categoryParts = s.Split('_');
                    if (categoryParts.Length >= 4 &&
                        int.TryParse(categoryParts[2], out int detailYear) &&
                        int.TryParse(categoryParts[3], out int detailMonth) &&
                        int.TryParse(categoryParts[4], out int detailCategoryId))
                    {
                        await _reportService.ShowCategoryDetails(chatId, detailYear, detailMonth, detailCategoryId);
                    }
                    break;

                // В CallbackHandler.cs в метод HandleCallbackQuery добавить новые cases
                case string s when s.StartsWith("detailed_report_"):
                    var detailedParts = s.Split('_');
                    if (detailedParts.Length >= 3 &&
                        int.TryParse(detailedParts[2], out int detailedYear) &&
                        int.TryParse(detailedParts[3], out int detailedMonth))
                    {
                        await _reportService.GenerateDetailedReport(chatId, detailedYear, detailedMonth);
                    }
                    break;

                case string s when s.StartsWith("back_to_report_"):
                    var backParts = s.Split('_');
                    if (backParts.Length >= 4 &&
                        int.TryParse(backParts[3], out int backYear) &&
                        int.TryParse(backParts[4], out int backMonth))
                    {
                        await _reportService.GenerateReport(chatId, backYear, backMonth);
                    }
                    break;

                case "no_action":
                    // Ничего не делаем при нажатии на разделитель
                    break;

                default:
                    // Логируем неизвестный callback для отладки
                    Console.WriteLine($"Неизвестный callback: {data}");
                    break;
            }
        }

        private async Task ShowYearMonths(long chatId, int year)
        {
            var yearsMonths = _budgetService.GetTransactionYearsMonths();

            if (!yearsMonths.ContainsKey(year) || yearsMonths[year].Count == 0)
            {
                await _bot.SendTextMessageAsync(chatId, $"Нет данных за {year} год.");
                return;
            }

            var months = yearsMonths[year];

            await _bot.SendTextMessageAsync(chatId, $"Выберите месяц за {year} год или выберите другой год:",
                replyMarkup: Keyboards.CreateYearMonthSelectionKB(year, months));
        }
       
        private async Task ShowYearSelection(long chatId)
        {
            var yearsMonths = _budgetService.GetTransactionYearsMonths();
            var years = yearsMonths.Keys.OrderDescending().ToList();

            if (years.Count == 0)
            {
                await _bot.SendTextMessageAsync(chatId, "Нет данных за любой год.");
                return;
            }

            await _bot.SendTextMessageAsync(chatId, "Выберите год:",
                replyMarkup: Keyboards.CreateYearsSelectionKB(years));
        }


        public void ResetLastSelectedYear(long chatId)
        {
            if (_lastSelectedYear.ContainsKey(chatId))
            {
                _lastSelectedYear.Remove(chatId);
            }
        }

    }

    }

