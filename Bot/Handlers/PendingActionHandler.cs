using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TGBotLog.Data.Models;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class PendingActionHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly Dictionary<long, (string Action, int? CategoryId)> _pendingActions = new();

        public PendingActionHandler(ITelegramBotClient bot, BudgetService budgetService)
        {
            _bot = bot;
            _budgetService = budgetService;
        }

        public bool HasPendingAction(long chatId) => _pendingActions.ContainsKey(chatId);

        public (string Action, int? CategoryId) GetPendingAction(long chatId)
        {
            if (_pendingActions.TryGetValue(chatId, out var pending))
                return pending;
            return (null, null);
        }

        public void SetPendingAction(long chatId, string action, int? categoryId = null)
        {
            _pendingActions[chatId] = (action, categoryId);
        }

        public void RemovePendingAction(long chatId)
        {
            if (_pendingActions.ContainsKey(chatId))
            {
                _pendingActions.Remove(chatId);
            }
        }

        public async Task HandlePendingAction(long chatId, string text)
        {
            
            if (!_pendingActions.TryGetValue(chatId, out var pending))
                
            return;

            switch (pending.Action)
            {
                case "SELECT_CATEGORY_TYPE":
                    await HandleCategoryTypeSelection(chatId, text);

                    break;

                case "ADD_CATEGORY":
                    RemovePendingAction(chatId);
                    if (text == "ДОХОД" || text == "РАСХОД" || text == "ОТМЕНА")
                    {
                        await _bot.SendTextMessageAsync(chatId, "ОПЕРАЦИЯ ОТМЕНЕНА", replyMarkup: Keyboards.MainMenu);
                        return;
                    }

                    TransactionType categoryType = (TransactionType)(pending.CategoryId ?? 0);
                    _budgetService.AddCategory(text.ToUpper(), categoryType);
                    
                    
                    

                    string typeName = categoryType == TransactionType.Income ? "ДОХОДОВ" : "РАСХОДОВ";
                    
                    await _bot.SendTextMessageAsync(chatId, $"✅ Категория {typeName} '{text.ToUpper()}' добавлена!", replyMarkup: Keyboards.MainMenu);
                    break;
            }
        }

        public async Task HandleCategoryTypeSelection(long chatId, string text)
        {
            TransactionType selectedType;

            switch (text)
            {
                case "РАСХОД":
                    selectedType = TransactionType.Expense;
                    break;
                case "ДОХОД":
                    selectedType = TransactionType.Income;
                    break;

                default:
                    await _bot.SendTextMessageAsync(chatId, "ГЛАВНОЕ МЕНЮ", replyMarkup: Keyboards.MainMenu);
                    RemovePendingAction(chatId);
                    return;
            }

            await _bot.SendTextMessageAsync(chatId, "Введите название категории:");
            _pendingActions[chatId] = ("ADD_CATEGORY", (int)selectedType);
        }

        public async Task ShowCategoryTypeSelection(long chatId)
        {
            var typeMenu = @"📁 <b>Выберите тип категории:</b>";


            await _bot.SendTextMessageAsync(
               chatId,
               "_____",
               parseMode: ParseMode.Html,
               replyMarkup: Keyboards.SelectTypeOfCategorie
           );

            await _bot.SendTextMessageAsync(
                chatId,
                typeMenu,
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.SelectTypeOFCategorie
            );

            _pendingActions[chatId] = ("SELECT_CATEGORY_TYPE", null);
        }
    }
}