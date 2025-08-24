namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// Тип финансовой операции
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Расход (трата денег)
        /// </summary>
        Expense = 0,

        /// <summary>
        /// Доход (поступление денег)
        /// </summary>
        Income = 1,

        /// <summary>
        /// Накопление (отложенные средства)
        /// </summary>
        Saving = 2,

        /// <summary>
        /// Перевод между счетами
        /// </summary>
        Transfer = 3
    }
}