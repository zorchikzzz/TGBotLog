using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using FamilyBudgetBot.Services;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;

        public CommandHandler(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
        }

        public async Task HandleCommand(long chatId, string command)
        {
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

        private async Task ShowMainMenu(long chatId)
        {
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

            await _bot.SendTextMessageAsync(
                chatId,
                menu,
                parseMode: ParseMode.Html
            );
        }

        private async Task ShowHelp(long chatId)
        {
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

            await _bot.SendTextMessageAsync(
                chatId,
                helpText,
                parseMode: ParseMode.Html
            );
        }

        private async Task GenerateReport(long chatId)
        {
            var transactions = _budgetService.GetTransactions(
                DateTime.Now.AddMonths(-1),
                DateTime.Now
            );

            if (!transactions.Any())
            {
                await _bot.SendTextMessageAsync(chatId, "📭 Нет данных за последний месяц");
                return;
            }

            var categories = _budgetService.GetAllCategories();
            var report = transactions
                .GroupBy(t => t.CategoryId)
                .Select(g => {
                    var category = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Неизвестная";
                    return new
                    {
                        Category = category,
                        Total = g.Sum(t => t.Amount)
                    };
                })
                .OrderByDescending(r => r.Total);

            var totalExpenses = report.Sum(r => r.Total);
            var message = $"📈 <b>Отчет за последний месяц</b>\nОбщие расходы: {totalExpenses:C}\n\n" +
                          "<b>По категориям:</b>\n" +
                          string.Join("\n", report.Select(r => $"- {r.Category}: {r.Total:C}"));

            await _bot.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Html
            );
        }
    }
}