namespace FamilyBudgetBot.Data.Models
{
    /// Модель категории расходов/доходов
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// Тип операций для этой категории
        public TransactionType Type { get; set; }

        /// Цвет категории для визуализации (опционально)
        public string Color { get; set; } = "#3498db";

        /// Иконка категории (опционально)
        public string Icon { get; set; } = "📁";
    }
}