namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// Модель категории расходов/доходов
    /// </summary>
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Тип операций для этой категории
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Цвет категории для визуализации (опционально)
        /// </summary>
        public string Color { get; set; } = "#3498db";

        /// <summary>
        /// Иконка категории (опционально)
        /// </summary>
        public string Icon { get; set; } = "📁";
    }
}