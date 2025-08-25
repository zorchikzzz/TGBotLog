using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Data.Repositories;
using System.Globalization;

namespace FamilyBudgetBot.Services
{
    /// Сервис для работы с бизнес-логикой приложения
    /// Содержит основную логику обработки данных между репозиторием и контроллерами
    public class BudgetService
    {
        // Репозиторий для работы с базой данных
        private readonly BudgetRepository _repository;

        /// Конструктор сервиса
        public BudgetService(BudgetRepository repository)
        {
            // Сохраняем переданный репозиторий для использования в методах сервиса
            _repository = repository;
        }

        /// Добавление новой категории
        public int AddCategory(string name, TransactionType type = TransactionType.Expense, string color = "#3498db", string icon = "📁")
        {
            return _repository.AddCategory(name, type, color, icon);
        }


        /// Получение всех категорий
        public List<Category> GetAllCategories() => _repository.GetAllCategories();

        /// Получение категории по ID
        public Category? GetCategoryById(int id) => _repository.GetCategoryById(id);

        /// Получение транзакций за указанный период
        public List<Transaction> GetTransactions(DateTime? startDate, DateTime? endDate) =>
            _repository.GetTransactions(startDate, endDate);

        /// Обработка текстового сообщения с транзакцией
        /// Разбирает сообщение формата "1500 продукты покупки в магазине"
        public (bool Success, string Message) ProcessTransactionMessage(string messageText)
        {
            try
            {
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return (false, "Неверный формат. Пример: 1500 зарплата аванс или 1500 продукты покупки");

                
                TransactionType transactionType;
                string amountString = parts[0];


                if (!decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    return (false, "Не удалось распознать сумму. Пример: 1500 зарплата или 1500 продукты");

                if (amount <= 0)
                    return (false, "Сумма должна быть положительной во избежание ошибок, если необходимо добавить доход или расход то укажите категорию соответсвующего типа");

                string categoryName = parts[1];
                string description = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Без описания";

                var category = _repository.GetCategoryByName(categoryName);
                
                
                if (category == null)
                {
                    return (false, $"❌ Категория '{categoryName}' не найдена. " +
                                   "Сначала добавьте категорию с помощью команды /addcategory");
                }
                transactionType = category.Type;
                
                
                var transaction = new Transaction
                {
                    Amount = amount,
                    CategoryId = category.Id,
                    Date = DateTime.Now,
                    Description = description,
                    Type = transactionType
                };

                _repository.AddTransaction(transaction);

                string typeEmoji = transactionType == TransactionType.Income ? "💰" : "💸";

                return (true, $"{typeEmoji} Транзакция добавлена: {amount} руб. в категории '{categoryName}'");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при обработке транзакции: {ex.Message}");
            }
        }

        public List<Category> GetCategoriesByType(TransactionType type)
        {
            return _repository.GetAllCategories().Where(c => c.Type == type).ToList();
        }
    }
}