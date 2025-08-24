using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Data.Repositories;
using System.Globalization;

namespace FamilyBudgetBot.Services
{
    public class BudgetService
    {
        private readonly BudgetRepository _repository;

        public BudgetService(BudgetRepository repository)
        {
            _repository = repository;
        }

        public int AddCategory(string name) => _repository.AddCategory(name);
        public int AddTransaction(Transaction transaction) => _repository.AddTransaction(transaction);
        public List<Category> GetAllCategories() => _repository.GetAllCategories();
        public Category? GetCategoryById(int id) => _repository.GetCategoryById(id);
        public List<Transaction> GetTransactions(DateTime? startDate, DateTime? endDate) =>
            _repository.GetTransactions(startDate, endDate);

        // Новый метод для обработки транзакций из текстового сообщения
        public (bool Success, string Message) ProcessTransactionMessage(string messageText)
        {
            try
            {
                // Разделяем сообщение на части
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return (false, "Неверный формат. Пример: 1500 продукты покупки в магазине");

                // Парсим сумму
                if (!decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    return (false, "Не удалось распознать сумму. Пример: 1500 продукты покупки в магазине");

                // Извлекаем категорию (второе слово)
                string categoryName = parts[1];

                // Извлекаем описание (остальные слова)
                string description = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Без описания";

                // Ищем категорию по имени
                var category = _repository.GetCategoryByName(categoryName);
                if (category == null)
                {
                    // Если категория не найдена, создаем новую
                    category = new Category { Name = categoryName };
                    category.Id = _repository.AddCategory(categoryName);
                }

                // Создаем транзакцию
                var transaction = new Transaction
                {
                    Amount = amount,
                    CategoryId = category.Id,
                    Date = DateTime.Now,
                    Description = description
                };

                // Добавляем транзакцию в базу
                _repository.AddTransaction(transaction);

                return (true, $"✅ Транзакция добавлена: {amount} руб. в категории '{categoryName}'");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при обработке транзакции: {ex.Message}");
            }
        }
    }
}