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
        public int AddCategory(string name) => _repository.AddCategory(name);

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
                // Разделяем сообщение на части по пробелам
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Проверяем, что сообщение содержит как минимум сумму и категорию
                if (parts.Length < 2)
                    return (false, "Неверный формат. Пример: 1500 продукты покупки в магазине");

                // Пытаемся распарсить сумму из первой части сообщения
                if (!decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    return (false, "Не удалось распознать сумму. Пример: 1500 продукты покупки в магазине");

                // Извлекаем название категории из второй части сообщения
                string categoryName = parts[1];

                // Формируем описание из оставшихся частей сообщения (если они есть)
                string description = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Без описания";

                // Ищем категорию по имени в базе данных
                var category = _repository.GetCategoryByName(categoryName);

                // Если категория не найдена, возвращаем ошибку
                if (category == null)
                {
                    return (false, $"❌ Категория '{categoryName}' не найдена. " +
                                   "Сначала добавьте категорию с помощью команды /addcategory");
                }

                // Создаем объект транзакции с полученными данными
                var transaction = new Transaction
                {
                    Amount = amount,
                    CategoryId = category.Id,
                    Date = DateTime.Now,
                    Description = description
                };

                // Добавляем транзакцию в базу данных
                _repository.AddTransaction(transaction);

                // Возвращаем успешный результат с сообщением для пользователя
                return (true, $"✅ Транзакция добавлена: {amount} руб. в категории '{categoryName}'");
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем сообщение об ошибке
                return (false, $"Ошибка при обработке транзакции: {ex.Message}");
            }
        }
    }
}