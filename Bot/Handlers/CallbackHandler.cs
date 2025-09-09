using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using TGBotLog.Data.Models ;


namespace TGBotLog.Bot.Handlers
{
    public class CallbackHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly CommandHandler _commandHandler;
        private readonly BudgetService _budgetService;
        // словарь для хранения последнего выбранного года
        private readonly Dictionary<long, int> _lastSelectedYear = new Dictionary<long, int>();

        public CallbackHandler(ITelegramBotClient bot, PendingActionHandler pendingActionHandler,
            CommandHandler commandHandler, BudgetService budgetService)
        {
            _bot = bot;
            _pendingActionHandler = pendingActionHandler;
            _budgetService = budgetService;
            _commandHandler = commandHandler;
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

                // Новые обработчики для выбора периода
                case "select_report_period":
                    // Используем последний выбранный год, если он есть, иначе текущий год
                    int yearToShow = _lastSelectedYear.ContainsKey(chatId)
                        ? _lastSelectedYear[chatId]
                        : DateTime.Now.Year;
                    await ShowYearMonths(chatId, yearToShow);
                    break;

                case "select_report_year":
                    await ShowYearSelection(chatId);
                    break;

                // В методе HandleCallbackQuery обновим обработчики
                case string s when s.StartsWith("select_month_"):
                    if (int.TryParse(s.Substring("select_month_".Length), out int year))
                    {
                        // Сохраняем выбранный год
                        _lastSelectedYear[chatId] = year;
                        await ShowMonthSelection(chatId, year);
                    }
                    break;

                case string s when s.StartsWith("report_month_"):
                    var parts = s.Split('_');
                    if (parts.Length >= 4 && int.TryParse(parts[2], out int reportYear) &&
                        int.TryParse(parts[3], out int reportMonth))
                    {
                        // Сохраняем выбранный год
                        _lastSelectedYear[chatId] = reportYear;
                        await _commandHandler.GenerateReport(chatId, reportYear, reportMonth);
                    }
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
        private async Task ShowMonthSelection(long chatId, int year)
        {
            var yearsMonths = _budgetService.GetTransactionYearsMonths();
            if (!yearsMonths.ContainsKey(year) || yearsMonths[year].Count == 0)
            {
                await _bot.SendTextMessageAsync(chatId, "Данные за выбранный год не найдены.");
                return;
            }

            var months = yearsMonths[year];
            
            await _bot.SendTextMessageAsync(chatId, $"Выберите месяц за {year} год:",
                replyMarkup: Keyboards.CreateMonthSelectionKB(months, year));
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

