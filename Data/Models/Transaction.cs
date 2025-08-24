namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// Модель финансовой операции (транзакции)
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Тип транзакции (определяется автоматически из категории)
        /// </summary>
        public TransactionType Type { get; set; }
    }
}