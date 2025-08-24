using System;

namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// Модель финансовой операции (транзакции)
    /// Представляет собой запись о доходе или расходе средств
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Уникальный идентификатор транзакции
        /// Автоматически генерируется базой данных при добавлении
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Сумма транзакции (положительное число)
        /// Для расходов обычно используется положительное значение
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Дата и время совершения транзакции
        /// По умолчанию используется текущее время при создании
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Идентификатор категории, к которой относится транзакция
        /// Связывает транзакцию с определенной категорией
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Дополнительное описание или комментарий к транзакции
        /// Может содержать details о том, на что именно были потрачены средства
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}