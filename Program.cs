using FamilyBudgetBot.Data.Repositories;
using FamilyBudgetBot.Services;
using FamilyBudgetBot.Bot.Services;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Инициализация зависимостей
        var repository = new BudgetRepository();
        var budgetService = new BudgetService(repository);
        var botService = new TelegramBotService("5829830933:AAFm29KTHLtOFoF4YM5_Kq_GN2OEhHKR_oU", budgetService);

        // Запуск бота
        botService.Start();

        Console.WriteLine("Приложение для управления бюджетом запущено...");
        Console.WriteLine("Нажмите Enter для выхода");
        Console.ReadLine();
    }
}