using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;

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
            var buttons = new List<InlineKeyboardButton[]>();

            // Добавляем кнопки для каждого месяца указанного года
            foreach (var month in months)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                $"{monthName}",
                $"report_month_{year}_{month}")
        });
            }

            // Добавляем кнопку "Выбрать год"
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("Выбрать год", "select_report_year")
    });

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await _bot.SendTextMessageAsync(chatId, $"Выберите месяц за {year} год или выберите другой год:",
                replyMarkup: inlineKeyboard);
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

            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var year in years)
            {
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                year.ToString(),
                $"select_month_{year}")
        });
            }

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await _bot.SendTextMessageAsync(chatId, "Выберите год:",
                replyMarkup: inlineKeyboard);
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
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var month in months)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                $"{monthName}",
                $"report_month_{year}_{month}")
        });
            }

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await _bot.SendTextMessageAsync(chatId, $"Выберите месяц за {year} год:",
                replyMarkup: inlineKeyboard);
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

