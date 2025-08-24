using Telegram.Bot;
using FamilyBudgetBot.Services;

namespace FamilyBudgetBot.Bot.Handlers
{
    /// <summary>
    /// Обработчик ожидаемых действий (многошаговых операций)
    /// Управляет состояниями диалога с пользователем
    /// </summary>
    public class PendingActionHandler
    {
        // Клиент Telegram Bot API для отправки сообщений
        private readonly ITelegramBotClient _bot;

        // Сервис для работы с бизнес-логикой приложения
        private readonly BudgetService _budgetService;

        // Словарь для хранения состояний диалога с пользователями
        // Ключ: ID чата, Значение: (действие, ID категории)
        private readonly Dictionary<long, (string Action, int? CategoryId)> _pendingActions = new();

        /// <summary>
        /// Конструктор обработчика ожидаемых действий
        /// </summary>
        /// <param name="bot">Клиент Telegram Bot API</param>
        /// <param name="budgetService">Сервис для работы с бизнес-логикой</param>
        public PendingActionHandler(ITelegramBotClient bot, BudgetService budgetService)
        {
            // Сохраняем переданные зависимости для использования в методах
            _bot = bot;
            _budgetService = budgetService;
        }

        /// <summary>
        /// Проверить, есть ли ожидаемое действие для указанного чата
        /// </summary>
        /// <param name="chatId">ID чата для проверки</param>
        /// <returns>True, если есть ожидаемое действие, иначе False</returns>
        public bool HasPendingAction(long chatId) => _pendingActions.ContainsKey(chatId);

        /// <summary>
        /// Установить ожидаемое действие для указанного чата
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="action">Тип действия</param>
        /// <param name="categoryId">ID категории (опционально)</param>
        public void SetPendingAction(long chatId, string action, int? categoryId = null)
        {
            _pendingActions[chatId] = (action, categoryId);
        }

        /// <summary>
        /// Обработать ожидаемое действие
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="text">Текст сообщения от пользователя</param>
        public async Task HandlePendingAction(long chatId, string text)
        {
            // Получаем ожидаемое действие для указанного чата
            if (!_pendingActions.TryGetValue(chatId, out var pending))
                return;

            // Обрабатываем действие в зависимости от его типа
            switch (pending.Action)
            {
                case "ADD_CATEGORY":
                    // Добавляем новую категорию
                    _budgetService.AddCategory(text);

                    // Удаляем ожидаемое действие
                    _pendingActions.Remove(chatId);

                    // Отправляем подтверждение пользователю
                    await _bot.SendTextMessageAsync(chatId, $"✅ Категория '{text}' добавлена!");
                    break;

                    // Другие типы действий можно добавить здесь при расширении функционала
            }
        }
    }
}