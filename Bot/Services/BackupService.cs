using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Services;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
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
                _budgetService.CloseConnection();
                await Task.Delay(1000);

                if (!System.IO.File.Exists(_dbPath))
                {
                    await _bot.SendTextMessageAsync(chatId, "❌ Файл базы данных не найден");
                    return;
                }

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
                // 1. Проверка расширения файла
                var fileName = document.FileName ?? "unknown";
                if (!fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    await _bot.SendTextMessageAsync(chatId,
                        "❌ Неподдерживаемый формат файла. Ожидается файл с расширением .db или .sqlite.");
                    _pendingActionHandler.RemovePendingAction(chatId);
                    return;
                }

                var file = await _bot.GetFileAsync(document.FileId);

                // 2. Скачиваем во временный файл
                var tempPath = Path.GetTempFileName();
                await using (var saveStream = System.IO.File.OpenWrite(tempPath))
                {
                    await _bot.DownloadFileAsync(file.FilePath, saveStream);
                }

                // 3. Проверяем, что это валидная SQLite-база
                if (!IsValidSqliteDatabase(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                    await _bot.SendTextMessageAsync(chatId,
                        "❌ Файл не является корректной базой данных SQLite.");
                    _pendingActionHandler.RemovePendingAction(chatId);
                    return;
                }

                // 4. Создаём бэкап текущей базы (если существует)
                var backupPath = _dbPath + ".backup";
                if (System.IO.File.Exists(_dbPath))
                {
                    System.IO.File.Copy(_dbPath, backupPath, true);
                }

                // 5. Заменяем базу данных
                System.IO.File.Copy(tempPath, _dbPath, true);
                System.IO.File.Delete(tempPath);

                await _bot.SendTextMessageAsync(chatId,
                    "✅ База данных успешно восстановлена! Бот будет перезапущен.");

                // 6. Перезапускаем приложение
                await Task.Delay(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Восстанавливаем из бэкапа при ошибке
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

        // Проверка, является ли файл валидной SQLite-базой
        private bool IsValidSqliteDatabase(string filePath)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={filePath};");
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master LIMIT 1";
                cmd.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}