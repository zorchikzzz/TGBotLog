namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// ������ ���������� �������� (����������)
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// ��� ���������� (������������ ������������� �� ���������)
        /// </summary>
        public TransactionType Type { get; set; }
    }
}