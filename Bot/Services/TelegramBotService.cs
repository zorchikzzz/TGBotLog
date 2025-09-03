using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Bot.Handlers;
using TGBotLog.Bot.Handlers;

namespace FamilyBudgetBot.Bot.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly CallbackHandler _callbackHandler;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BackupHandler _backupHandler;
        private readonly SqlQueryHandler _sqlQueryHandler;
        private readonly CommandHandler _commandHandler;

        public TelegramBotService(string botToken, BudgetService budgetService, string dbPath)
        {
            _bot = new TelegramBotClient(botToken);
            _budgetService = budgetService;

            // Сначала инициализируем все зависимости
            _cancellationTokenSource = new CancellationTokenSource();
            _pendingActionHandler = new PendingActionHandler(_bot, _budgetService);
            _backupHandler = new BackupHandler(_bot, _budgetService, _pendingActionHandler, dbPath);
            _sqlQueryHandler = new SqlQueryHandler(_bot, dbPath);

            // Теперь инициализируем CommandHandler с правильными зависимостями
            _commandHandler = new CommandHandler(_bot, _budgetService, _pendingActionHandler, _backupHandler, dbPath);

            // Инициализируем CallbackHandler с необходимыми зависимостями
            _callbackHandler = new CallbackHandler(_bot, _pendingActionHandler, _commandHandler);
        }


        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cancellationTokenSource.Token
            );
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.CallbackQuery != null)
            {
                await _callbackHandler.HandleCallbackQuery(update.CallbackQuery);
                return;
            }

            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id;


            try
            {
                // Обработка документов (для восстановления БД)
                if (message.Document != null)
                {
                    var pendingAction = _pendingActionHandler.GetPendingAction(chatId);
                    if (pendingAction.Action == "WAITING_RESTORE_FILE")
                    {
                        await _backupHandler.HandleDatabaseRestore(chatId, message.Document);
                        return;
                    }
                }



                // Обработка текстовых сообщений
                if (message.Text is not { } messageText)
                    return;

                // Сначала проверяем ожидающие действия
                if (_pendingActionHandler.HasPendingAction(chatId))
                {
                    await _pendingActionHandler.HandlePendingAction(chatId, messageText);
                    return;
                }

                // Затем обрабатываем команды
                if (messageText.StartsWith('/') )
                {
                    await _commandHandler.HandleCommand(chatId, messageText);
                    return;
                }

                // ПРОСТАЯ ПРОВЕРКА: если сообщение начинается с SELECT
                if (messageText.Trim().ToUpper().StartsWith("SELECT"))
                {
                    await _sqlQueryHandler.HandleSqlQuery(chatId, messageText);
                    return;
                }
                
                
                if (messageText == "ОТЧЁТ")
                {
                    await _commandHandler.GenerateReport(chatId);
                    return;
                }

                if (messageText == "КАТЕГОРИИ")
                {
                    await _commandHandler.ShowExpenseCategories(chatId);
                    return;
                }

                if (messageText == "СПРАВКА")
                    {
                        await _commandHandler.ShowHelp(chatId);
                        return;
                    }
                
                

                if (messageText == "ДОБАВИТЬ КАТЕГОРИЮ")
                {
                    await _pendingActionHandler.ShowCategoryTypeSelection(chatId);
                    return;
                }

              




                // Обработка транзакций
                var result = _budgetService.ProcessTransactionMessage(messageText);
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: result.Message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Произошла ошибка: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}