using System.Globalization;
using System.IO;
using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Services;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TGBotLog.Bot.Services
{
    public class BackupService
    {
        private readonly ITelegramBotClient _bot;
        private readonly BudgetService _budgetService;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly string _dbPath;

        public BackupService(ITelegramBotClient bot, BudgetService budgetService, PendingActionHandler pendingActionHandler, string dbPath)
        {
            _bot = bot;
            _budgetService = budgetService;
            _pendingActionHandler = pendingActionHandler;
            _dbPath = dbPath;
        }

    
        public async Task SendDatabaseBackup(long chatId)
        {
            try
            {
                // Закрываем все соединения с БД
                _budgetService.CloseConnection();

                // Даем время на освобождение файла
                await Task.Delay(1000);

                if (!System.IO.File.Exists(_dbPath))
                {
                    await _bot.SendTextMessageAsync(chatId, "❌ Файл базы данных не найден");
                    return;
                }

                // Создаем временную копию файла
                var tempBackupPath = Path.GetTempFileName();
                System.IO.File.Copy(_dbPath, tempBackupPath, true);

                await using (var stream = System.IO.File.OpenRead(tempBackupPath))
                {
                    await _bot.SendDocumentAsync(
                        chatId: chatId,
                        document: new InputOnlineFile(stream, "budget_backup.db"),
                        caption: "💾 Резервная копия базы данных"
                    );
                }

                // Удаляем временный файл
                System.IO.File.Delete(tempBackupPath);
            }
            catch (Exception ex)
            {
                await _bot.SendTextMessageAsync(chatId, $"❌ Ошибка при создании бэкапа: {ex.Message}");
            }
        }

        public async Task RequestDatabaseRestore(long chatId)
        {
            await _bot.SendTextMessageAsync(chatId,
                "📤 Отправьте файл базы данных для восстановления. Внимание: это перезапишет текущую базу данных!");

            _pendingActionHandler.SetPendingAction(chatId, "WAITING_RESTORE_FILE", null);
        }

        public async Task HandleDatabaseRestore(long chatId, Document document)
        {
            try
            {
                var file = await _bot.GetFileAsync(document.FileId);

                // Создаем временную копию текущей БД на случай ошибки
                var tempBackupPath = _dbPath + ".backup";
                if (System.IO.File.Exists(_dbPath))
                {
                    System.IO.File.Copy(_dbPath, tempBackupPath, true);
                }

                // Скачиваем и сохраняем новую БД
                await using (var saveStream = System.IO.File.OpenWrite(_dbPath))
                {
                    await _bot.DownloadFileAsync(file.FilePath, saveStream);
                }

                await _bot.SendTextMessageAsync(chatId,
                    "✅ База данных успешно восстановлена! Бот будет перезапущен.");

                // Перезапускаем приложение
                await Task.Delay(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Восстанавливаем из бекапа в случае ошибки
                if (System.IO.File.Exists(_dbPath + ".backup"))
                {
                    System.IO.File.Copy(_dbPath + ".backup", _dbPath, true);
                    System.IO.File.Delete(_dbPath + ".backup");
                }

                await _bot.SendTextMessageAsync(chatId,
                    $"❌ Ошибка при восстановлении базы данных: {ex.Message}");
            }
            finally
            {
                _pendingActionHandler.RemovePendingAction(chatId);
            }
        }
    }
}