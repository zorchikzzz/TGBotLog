namespace FamilyBudgetBot.Data.Models
{
    /// Тип финансовой операции
    public enum TransactionType
    {
        /// Расход (трата денег)
        Expense = 0,

        /// Доход (поступление денег)
        Income = 1,
       
        /// Накопление (отложенные средства)
        Saving = 2,

      
    }
}