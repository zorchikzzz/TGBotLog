using Telegram.Bot;
using FamilyBudgetBot.Services;

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

        public async Task HandlePendingAction(long chatId, string text)
        {
            if (!_pendingActions.TryGetValue(chatId, out var pending))
                return;

            switch (pending.Action)
            {
                case "ADD_CATEGORY":
                    _budgetService.AddCategory(text);
                    _pendingActions.Remove(chatId);
                    await _bot.SendTextMessageAsync(chatId, $"✅ Категория '{text}' добавлена!");
                    break;

                    // Убираем обработку транзакций, так как теперь они добавляются через обычные сообщения
            }
        }
    }
}