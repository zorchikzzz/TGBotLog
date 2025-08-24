using FamilyBudgetBot.Data.Models;
using FamilyBudgetBot.Data.Repositories;
using System.Globalization;

namespace FamilyBudgetBot.Services
{
    /// <summary>
    /// Сервис для работы с бизнес-логикой приложения
    /// Содержит основную логику обработки данных между репозиторием и контроллерами
    /// </summary>
    public class BudgetService
    {
        // Репозиторий для работы с базой данных
        private readonly BudgetRepository _repository;

        /// <summary>
        /// Конструктор сервиса
        /// </summary>
        /// <param name="repository">Экземпляр репозитория для работы с данными</param>
        public BudgetService(BudgetRepository repository)
        {
            // Сохраняем переданный репозиторий для использования в методах сервиса
            _repository = repository;
        }

        /// <summary>
        /// Добавление новой категории
        /// </summary>
        /// <param name="name">Название новой категории</param>
        /// <returns>ID добавленной категории</returns>
        public int AddCategory(string name, TransactionType type = TransactionType.Expense, string color = "#3498db", string icon = "📁")
        {
            return _repository.AddCategory(name, type, color, icon);
        }

        /// <summary>
        /// Добавление новой транзакции
        /// </summary>
        /// <param name="transaction">Объект транзакции для добавления</param>
        /// <returns>ID добавленной транзакции</returns>
        public int AddTransaction(Transaction transaction) => _repository.AddTransaction(transaction);

        /// <summary>
        /// Получение всех категорий
        /// </summary>
        /// <returns>Список всех категорий</returns>
        public List<Category> GetAllCategories() => _repository.GetAllCategories();

        /// <summary>
        /// Получение категории по ID
        /// </summary>
        /// <param name="id">ID категории для поиска</param>
        /// <returns>Найденная категория или null, если категория не найдена</returns>
        public Category? GetCategoryById(int id) => _repository.GetCategoryById(id);

        /// <summary>
        /// Получение транзакций за указанный период
        /// </summary>
        /// <param name="startDate">Начальная дата периода</param>
        /// <param name="endDate">Конечная дата периода</param>
        /// <returns>Список транзакций за указанный период</returns>
        public List<Transaction> GetTransactions(DateTime? startDate, DateTime? endDate) =>
            _repository.GetTransactions(startDate, endDate);

        /// <summary>
        /// Обработка текстового сообщения с транзакцией
        /// Разбирает сообщение формата "1500 продукты покупки в магазине"
        /// </summary>
        /// <param name="messageText">Текст сообщения с транзакцией</param>
        /// <returns>Результат обработки (успех/неудача и сообщение для пользователя)</returns>
        public (bool Success, string Message) ProcessTransactionMessage(string messageText)
        {
            try
            {
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return (false, "Неверный формат. Пример: +1500 зарплата аванс или -1500 продукты покупки");

                // Определяем тип транзакции по первому символу
                TransactionType transactionType;
                string amountString;

                if (parts[0].StartsWith("+"))
                {
                    transactionType = TransactionType.Income;
                    amountString = parts[0].Substring(1);
                }
                else if (parts[0].StartsWith("-"))
                {
                    transactionType = TransactionType.Expense;
                    amountString = parts[0].Substring(1);
                }
                else
                {
                    // По умолчанию считаем расходом
                    transactionType = TransactionType.Expense;
                    amountString = parts[0];
                }

                if (!decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    return (false, "Не удалось распознать сумму. Пример: +1500 зарплата или -1500 продукты");

                string categoryName = parts[1];
                string description = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Без описания";

                var category = _repository.GetCategoryByName(categoryName);
                if (category == null)
                {
                    return (false, $"❌ Категория '{categoryName}' не найдена. " +
                                   "Сначала добавьте категорию с помощью команды /addcategory");
                }

                // Проверяем соответствие типа операции и типа категории
                if (category.Type != transactionType)
                {
                    string typeName = transactionType == TransactionType.Income ? "доход" : "расход";
                    string categoryTypeName = category.Type == TransactionType.Income ? "доходов" : "расходов";

                    return (false, $"❌ Несоответствие типов. Вы пытаетесь добавить {typeName} в категорию {categoryTypeName}.");
                }

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
                return (true, $"{typeEmoji} Транзакция добавлена: {(transactionType == TransactionType.Income ? "+" : "-")}{amount} руб. в категории '{categoryName}'");
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