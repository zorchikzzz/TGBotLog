using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyBudgetBot.Bot.Handlers
{
    public class SqlQueryHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly string _dbPath;

        public SqlQueryHandler(ITelegramBotClient bot, string dbPath)
        {
            _bot = bot;
            _dbPath = dbPath;
        }

        public async Task HandleSqlQuery(long chatId, string query)
        {
            try
            {
                var result = await ExecuteQuery(query);
                // Обрезаем длинные результаты чтобы не превысить лимит Telegram
                if (result.Length > 4000)
                    result = result.Substring(0, 4000) + "...\n\n⚠️ Результат обрезан";

                await _bot.SendTextMessageAsync(chatId, $"📊 Результат:\n<code>{result}</code>",
                    parseMode: ParseMode.Html);
            }
            catch (Exception ex)
            {
                await _bot.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task<string> ExecuteQuery(string query)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath};");
            await connection.OpenAsync();

            using var command = new SqliteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var result = new System.Text.StringBuilder();

            // Заголовки столбцов
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Append(reader.GetName(i));
                if (i < reader.FieldCount - 1) result.Append(" | ");
            }
            result.AppendLine();
            result.AppendLine(new string('-', 50));

            // Данные
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                rowCount++;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.Append(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString());
                    if (i < reader.FieldCount - 1) result.Append(" | ");
                }
                result.AppendLine();

                // Ограничиваем количество строк
                if (rowCount >= 50)
                {
                    result.AppendLine("...\n⚠️ Показаны первые 50 строк");
                    break;
                }
            }

            if (rowCount == 0)
                result.Append("Нет данных");

            return result.ToString();
        }
    }
}