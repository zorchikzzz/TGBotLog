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

                case "SELECT_CATEGORY":
                    if (int.TryParse(text, out int categoryId))
                    {
                        var category = _budgetService.GetCategoryById(categoryId);
                        if (category != null)
                        {
                            await _bot.SendTextMessageAsync(
                                chatId,
                                $"📝 Выбрана категория: {category.Name}\nВведите сумму и описание через пробел:"
                            );
                            _pendingActions[chatId] = ("ADD_TRANSACTION", categoryId);
                        }
                        else
                        {
                            await _bot.SendTextMessageAsync(chatId, "❌ Категория не найдена");
                            await ShowCategorySelection(chatId);
                        }
                    }
                    else
                    {
                        await _bot.SendTextMessageAsync(chatId, "❌ Неверный формат. Введите номер категории:");
                        await ShowCategorySelection(chatId);
                    }
                    break;

                case "ADD_TRANSACTION":
                    var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1 && decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    {
                        var categoryIdValue = pending.CategoryId ?? 0;
                        var description = parts.Length > 1 ? parts[1] : "Без описания";

                        var category = _budgetService.GetCategoryById(categoryIdValue);

                        var transaction = new Transaction
                        {
                            Amount = amount,
                            Date = DateTime.Now,
                            CategoryId = categoryIdValue,
                            Description = description,
                            Type = category?.Type ?? TransactionType.Expense
                        };

                        _budgetService.AddTransaction(transaction);
                        RemovePendingAction(chatId);

                        string typeEmoji = transaction.Type == TransactionType.Income ? "💰" : "💸";
                        await _bot.SendTextMessageAsync(chatId, $"{typeEmoji} Транзакция добавлена!");
                    }
                    else
                    {
                        await _bot.SendTextMessageAsync(chatId, "❌ Неверный формат. Пример: 1500 Покупка продуктов");
                        await _bot.SendTextMessageAsync(
                            chatId,
                            $"Введите сумму и описание через пробел:"
                        );
                    }
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

        private async Task ShowCategorySelection(long chatId)
        {
            var categories = _budgetService.GetAllCategories();
            if (!categories.Any())
            {
                await _bot.SendTextMessageAsync(chatId, "ℹ️ Сначала добавьте категории через /addcategory");
                return;
            }

            var message = "Выберите категорию:\n" +
                          string.Join("\n", categories.Select(c => $"{c.Id}. {c.Name}")) +
                          "\n\nВведите номер категории:";

            await _bot.SendTextMessageAsync(chatId, message);
            _pendingActions[chatId] = ("SELECT_CATEGORY", null);
        }
    }
}