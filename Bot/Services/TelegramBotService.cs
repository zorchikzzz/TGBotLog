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
    /// �������� ������ ��� ������ � Telegram �����
    /// ������������ ������ ���� ����������� ����������
    /// </summary>
    public class TelegramBotService
    {
        // ������ Telegram Bot API ��� �������������� � Telegram
        private readonly ITelegramBotClient _bot;

        // ������ ��� ������ � ������-������� ����������
        private readonly BudgetService _budgetService;

        // ���������� ������ ����
        private readonly CommandHandler _commandHandler;

        // ���������� ��������� �������� (������������ ��������)
        private readonly PendingActionHandler _pendingActionHandler;

        /// <summary>
        /// ����������� ������� Telegram ����
        /// </summary>
        /// <param name="token">����� ����, ���������� �� BotFather</param>
        /// <param name="budgetService">������ ��� ������ � ������-�������</param>
        public TelegramBotService(string token, BudgetService budgetService)
        {
            // ������� ������ Telegram Bot API � �������������� ����������� ������
            _bot = new TelegramBotClient(token);

            // ��������� ������ ��� ������ � ������-�������
            _budgetService = budgetService;

            // ������� ���������� ��������� ��������
            _pendingActionHandler = new PendingActionHandler(_bot, _budgetService);

            // ������� ���������� ������
            _commandHandler = new CommandHandler(_bot, _budgetService, _pendingActionHandler);
        }

        /// <summary>
        /// ������ ���� � ������ ������������� �������� ���������
        /// </summary>
        public void Start()
        {
            // ����������� ��������� ��������� ����������
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()  // �������� ��� ���� ����������
            };

            // ��������� ������������� �������� ���������
            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,           // ���������� �������� ���������
                pollingErrorHandler: HandlePollingErrorAsync, // ���������� ������
                receiverOptions: receiverOptions           // ��������� ��������� ����������
            );

            // ������� ��������� � �������� ������� � �������
            Console.WriteLine("��� �������...");
        }

        /// <summary>
        /// ���������� �������� ���������� �� Telegram
        /// </summary>
        /// <param name="bot">������ Telegram Bot API</param>
        /// <param name="update">�������� ����������</param>
        /// <param name="cancellationToken">����� ������ ��� ����������� ��������</param>
        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            // ���������, ��� ���������� �������� ��������� ����������
            if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
                return;

            // ��������� ���������� � ���������
            var message = update.Message;
            var chatId = message.Chat.Id;
            var text = message.Text!;
                        
            
            try
            {
                // ���������, ���� �� ��������� �������� ��� ����� ����
                if (_pendingActionHandler.HasPendingAction(chatId))
                {
                    // ���� ����, ������������ ��������� ��� ����� �� ��������� ��������
                    await _pendingActionHandler.HandlePendingAction(chatId, text);
                    return;
                }

                // ���������, �������� �� ��������� �������� (���������� � "/")
                if (text.StartsWith("/"))
                {
                    // ���� ��� �������, �������� �� ����������� ������
                    await _commandHandler.HandleCommand(chatId, text);
                    return;
                }

                // ���� ��� �� ������� � �� ����� �� ��������� ��������,
                // �������� ���������� ��������� ��� ����������
                var result = _budgetService.ProcessTransactionMessage(text);

                // ���������� ��������� ��������� ������������
                await bot.SendTextMessageAsync(chatId, result.Message);
            }
            catch (Exception ex)
            {
                // � ������ ������ ���������� ��������� �� ������ ������������
                await bot.SendTextMessageAsync(chatId, $"������: {ex.Message}");
            }
        }

        /// <summary>
        /// ���������� ������, ����������� ��� ������ � Telegram Bot API
        /// </summary>
        /// <param name="bot">������ Telegram Bot API</param>
        /// <param name="exception">����������, ������� ���������</param>
        /// <param name="cancellationToken">����� ������ ��� ����������� ��������</param>
        private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            // ��������� ��������� �� ������ � ����������� �� ���� ����������
            var errorMessage = exception switch
            {
                // ������ API Telegram
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",

                // ����� ������ ����������
                _ => exception.ToString()
            };

            // ������� ��������� �� ������ � �������
            Console.WriteLine(errorMessage);

            // ���������� ����������� ������
            return Task.CompletedTask;
        }
    }
}