using Microsoft.Extensions.Configuration;
using FamilyBudgetBot.Data.Repositories;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Bot.Services;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        // Установка кодировки консоли для поддержки Unicode символов
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            // Создание и настройка конфигурации
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Инициализация зависимостей
            var repository = new BudgetRepository(configuration["Database:Path"]);
            var budgetService = new BudgetService(repository);

            // Получение токена бота из конфигурации
            var botToken = configuration["BotConfiguration:BotToken"];

            if (string.IsNullOrEmpty(botToken))
            {
                Console.WriteLine("Токен бота не найден в конфигурации. Убедитесь, что файл appsettings.json содержит токен бота.");
                Console.WriteLine("Нажмите Enter для выхода.");
                Console.ReadLine();
                return;
            }

            var botService = new TelegramBotService(botToken, budgetService);

            // Запуск бота
            botService.Start();

            Console.WriteLine("Приложение для управления бюджетом запущено...");
            Console.WriteLine("Нажмите Enter для выхода");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            // Обработка ошибок запуска приложения
            Console.WriteLine($"Критическая ошибка при запуске приложения: {ex.Message}");
            Console.WriteLine("Нажмите Enter для выхода.");
            Console.ReadLine();
        }
    }
}