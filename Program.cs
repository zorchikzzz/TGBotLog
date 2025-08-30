using Microsoft.Extensions.Configuration;
using FamilyBudgetBot.Data.Repositories;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Bot.Services;
using System;
using System.IO;
using System.Threading;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Используем путь к базе данных в директории приложения
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "budget.db");

            // Создаем директорию data, если она не существует
            var dataDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            var repository = new BudgetRepository(dbPath);
            var budgetService = new BudgetService(repository);

            var botToken = configuration["BotConfiguration:BotToken"];

            if (string.IsNullOrEmpty(botToken) || botToken.Length != 46)
            {
                Console.WriteLine("Токен бота не найден в конфигурации или имеет неверный формат");
                Console.WriteLine("Нажмите Enter для выхода.");
                Console.ReadLine();
                return;
            }

            var botService = new TelegramBotService(botToken, budgetService, dbPath);
            botService.Start();

            Console.WriteLine("Приложение для управления бюджетом запущено успешно...");

            // Бесконечное ожидание вместо Console.ReadLine()
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при запуске приложения: {ex.Message}");
            Console.WriteLine("Нажмите Enter для выхода.");
            Console.ReadLine();
        }
    }
}