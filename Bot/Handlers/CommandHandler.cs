using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using FamilyBudgetBot.Services;

namespace FamilyBudgetBot.Bot.Handlers
{
    /// <summary>
    /// Обработчик команд Telegram бота
    /// Отвечает за обработку команд, начинающихся с "/"
    /// </summary>
    public class CommandHandler
    {
        // Клиент Telegram Bot API для отправки сообщений
        private readonly ITelegramBotClient _bot;

        // Сервис для работы с бизнес-логикой приложения
        private readonly BudgetService _budgetService;

        // Обработчик ожидаемых действий (многошаговые операции)
        private readonly PendingActionHandler _pendingActionHandler;

        /// <summary>
        /// Конструктор обработчика команд
        /// </summary>
        /// <param name="bot">Клиент Telegram Bot API</param>
        /// <param name="budgetService">Сервис для работы с бизнес-логикой</param>
        /// <param name="pendingActionHandler">Обработчик ожидаемых действий</param>
        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler)
        {
            // Сохраняем переданные зависимости для использования в методах
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
        }

        /// <summary>
        /// Обработка входящей команды
        /// </summary>
        /// <param name="chatId">ID чата, из которого пришла команда</param>
        /// <param name="command">Текст команды</param>
        public async Task HandleCommand(long chatId, string command)
        {
            // Определяем тип команды и вызываем соответствующий обработчик
            switch (command.ToLower())
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    break;

                case "/addcategory":
                    await _bot.SendTextMessageAsync(chatId, "Введите название категории:");
                    _pendingActionHandler.SetPendingAction(chatId, "ADD_CATEGORY");
                    break;

                case "/report":
                    await GenerateReport(chatId);
                    break;

                case "/help":
                    await ShowHelp(chatId);
                    break;

                default:
                    await _bot.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help для списка команд.");
                    break;
            }
        }

        /// <summary>
        /// Показать главное меню бота
        /// </summary>
        /// <param name="chatId">ID чата, в который отправить меню</param>
        private async Task ShowMainMenu(long chatId)
        {
            // Формируем текст главного меню с использованием HTML-разметки
            var menu = @"📊 <b>Управление семейным бюджетом</b>

Доступные команды:
/addcategory - Добавить категорию
/report - Показать отчет
/help - Показать справку

📝 <b>Добавление транзакций:</b>
Просто напишите сообщение в формате:
<code>1500 продукты покупки в магазине</code>

Где:
• 1500 - сумма
• продукты - категория
• покупки в магазине - описание";

            // Отправляем меню пользователю
            await _bot.SendTextMessageAsync(
                chatId,
                menu,
                parseMode: ParseMode.Html  // Указываем, что текст содержит HTML-разметку
            );
        }

        /// <summary>
        /// Показать справку по использованию бота
        /// </summary>
        /// <param name="chatId">ID чата, в который отправить справку</param>
        private async Task ShowHelp(long chatId)
        {
            // Формируем текст справки с использованием HTML-разметки
            var helpText = @"🤖 <b>Справка по боту</b>

<b>Команды:</b>
/start - Главное меню
/addcategory - Добавить новую категорию
/report - Показать отчет за последний месяц
/help - Эта справка

<b>Добавление транзакций:</b>
Просто отправьте сообщение в формате:
<code>сумма категория описание</code>

<b>Примеры:</b>
<code>1500 продукты покупки в магазине</code>
<code>2500 аренда квартира</code>
<code>500 транспорт такси</code>

Категория будет создана автоматически, если её ещё нет.";

            // Отправляем справку пользователю
            await _bot.SendTextMessageAsync(
                chatId,
                helpText,
                parseMode: ParseMode.Html  // Указываем, что текст содержит HTML-разметку
            );
        }

        /// <summary>
        /// Сгенерировать и отправить отчет о транзакциях
        /// </summary>
        /// <param name="chatId">ID чата, в который отправить отчет</param>
        private async Task GenerateReport(long chatId)
        {
            // Получаем транзакции за последний месяц
            var transactions = _budgetService.GetTransactions(
                DateTime.Now.AddMonths(-1),  // Начальная дата: месяц назад
                DateTime.Now                 // Конечная дата: сейчас
            );

            // Проверяем, есть ли транзакции за указанный период
            if (!transactions.Any())
            {
                await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за последний месяц");
                return;
            }

            // Получаем все категории для сопоставления ID с названиями
            var categories = _budgetService.GetAllCategories();

            // Группируем транзакции по категориям и вычисляем общую сумму для каждой категории
            var report = transactions
                .GroupBy(t => t.CategoryId)  // Группируем по ID категории
                .Select(g => {
                    // Находим название категории по ID
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";

                    // Возвращаем объект с названием категории и общей суммой
                    return new
                    {
                        Category = category,
                        Total = g.Sum(t => t.Amount)  // Суммируем amount всех транзакций в группе
                    };
                })
                .OrderByDescending(r => r.Total);  // Сортируем по убыванию суммы

            // Вычисляем общую сумму всех расходов
            var totalExpenses = report.Sum(r => r.Total);

            // Формируем текст отчета
            var message = $"📈 <b>Отчет за последний месяц</b>\nОбщие расходы: {totalExpenses:C}\n\n" +
                          "<b>По категориям:</b>\n" +
                          string.Join("\n", report.Select(r => $"- {r.Category}: {r.Total:C}"));

            // Отправляем отчет пользователю
            await _bot.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Html  // Указываем, что текст содержит HTML-разметку
            );
        }
    }
}