using System;

namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// Модель категории расходов/доходов
    /// Представляет собой группу для объединения финансовых операций
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Уникальный идентификатор категории
        /// Автоматически генерируется базой данных при добавлении
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название категории (например, "Продукты", "Транспорт", "Развлечения")
        /// Должно быть уникальным в системе
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}