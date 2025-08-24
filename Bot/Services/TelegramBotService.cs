using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Bot.Handlers;

namespace FamilyBudgetBot.Bot.Services
{
    /// <summary>
    /// Основной сервис для работы с Telegram ботом
    /// Координирует работу всех компонентов приложения
    /// </summary>
    public class TelegramBotService
    {
        // Клиент Telegram Bot API для взаимодействия с Telegram
        private readonly ITelegramBotClient _bot;

        // Сервис для работы с бизнес-логикой приложения
        private readonly BudgetService _budgetService;

        // Обработчик команд бота
        private readonly CommandHandler _commandHandler;

        // Обработчик ожидаемых действий (многошаговых операций)
        private readonly PendingActionHandler _pendingActionHandler;

        /// <summary>
        /// Конструктор сервиса Telegram бота
        /// </summary>
        /// <param name="token">Токен бота, полученный от BotFather</param>
        /// <param name="budgetService">Сервис для работы с бизнес-логикой</param>
        public TelegramBotService(string token, BudgetService budgetService)
        {
            // Создаем клиент Telegram Bot API с использованием переданного токена
            _bot = new TelegramBotClient(token);

            // Сохраняем сервис для работы с бизнес-логикой
            _budgetService = budgetService;

            // Создаем обработчик ожидаемых действий
            _pendingActionHandler = new PendingActionHandler(_bot, _budgetService);

            // Создаем обработчик команд
            _commandHandler = new CommandHandler(_bot, _budgetService, _pendingActionHandler);
        }

        /// <summary>
        /// Запуск бота и начало прослушивания входящих сообщений
        /// </summary>
        public void Start()
        {
            // Настраиваем параметры получения обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()  // Получаем все типы обновлений
            };

            // Запускаем прослушивание входящих сообщений
            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,           // Обработчик входящих сообщений
                pollingErrorHandler: HandlePollingErrorAsync, // Обработчик ошибок
                receiverOptions: receiverOptions           // Параметры получения обновлений
            );

            // Выводим сообщение о успешном запуске в консоль
            Console.WriteLine("Бот запущен...");
        }

        /// <summary>
        /// Обработчик входящих обновлений от Telegram
        /// </summary>
        /// <param name="bot">Клиент Telegram Bot API</param>
        /// <param name="update">Входящее обновление</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            // Проверяем, что обновление является текстовым сообщением
            if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
                return;

            // Извлекаем информацию о сообщении
            var message = update.Message;
            var chatId = message.Chat.Id;
            var text = message.Text!;
                        
            
            try
            {
                // Проверяем, есть ли ожидаемое действие для этого чата
                if (_pendingActionHandler.HasPendingAction(chatId))
                {
                    // Если есть, обрабатываем сообщение как ответ на ожидаемое действие
                    await _pendingActionHandler.HandlePendingAction(chatId, text);
                    return;
                }

                // Проверяем, является ли сообщение командой (начинается с "/")
                if (text.StartsWith("/"))
                {
                    // Если это команда, передаем ее обработчику команд
                    await _commandHandler.HandleCommand(chatId, text);
                    return;
                }

                // Если это не команда и не ответ на ожидаемое действие,
                // пытаемся обработать сообщение как транзакцию
                var result = _budgetService.ProcessTransactionMessage(text);

                // Отправляем результат обработки пользователю
                await bot.SendTextMessageAsync(chatId, result.Message);
            }
            catch (Exception ex)
            {
                // В случае ошибки отправляем сообщение об ошибке пользователю
                await bot.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик ошибок, возникающих при работе с Telegram Bot API
        /// </summary>
        /// <param name="bot">Клиент Telegram Bot API</param>
        /// <param name="exception">Исключение, которое произошло</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            // Формируем сообщение об ошибке в зависимости от типа исключения
            var errorMessage = exception switch
            {
                // Ошибка API Telegram
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",

                // Любое другое исключение
                _ => exception.ToString()
            };

            // Выводим сообщение об ошибке в консоль
            Console.WriteLine(errorMessage);

            // Возвращаем завершенную задачу
            return Task.CompletedTask;
        }
    }
}