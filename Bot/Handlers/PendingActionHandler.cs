using System.Globalization;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

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
                    TransactionType categoryType = (TransactionType)(pending.CategoryId ?? 0);
                    _budgetService.AddCategory(text, categoryType);
                    RemovePendingAction(chatId);

                    string typeName = categoryType == TransactionType.Income ? "доходов" :
                                     categoryType == TransactionType.Expense ? "расходов" : "накоплений";
                    await _bot.SendTextMessageAsync(chatId, $"✅ Категория {typeName} '{text}' добавлена!");
                    break;
               
            }
        }

        private async Task HandleCategoryTypeSelection(long chatId, string text)
        {
            TransactionType selectedType;

            switch (text.ToLower())
            {
                case "/expense":
                    selectedType = TransactionType.Expense;
                    break;
                case "/income":
                    selectedType = TransactionType.Income;
                    break;
                case "/saving":
                    selectedType = TransactionType.Saving;
                    break;
                default:
                    await _bot.SendTextMessageAsync(chatId, "Неверный тип категории. Используйте /expense, /income или /saving");
                    await ShowCategoryTypeSelection(chatId);
                    return;
            }

            await _bot.SendTextMessageAsync(chatId, "Введите название категории:");
            _pendingActions[chatId] = ("ADD_CATEGORY", (int)selectedType);
        }

        public async Task ShowCategoryTypeSelection(long chatId)
        {
            var typeMenu = @"📁 <b>Выберите тип категории:</b>

/expense - Категория расходов 💸
/income - Категория доходов 💰
/saving - Категория накоплений 🏦";

            await _bot.SendTextMessageAsync(
                chatId,
                typeMenu,
                parseMode: ParseMode.Html
            );

            _pendingActions[chatId] = ("SELECT_CATEGORY_TYPE", null);
        }
    }
}