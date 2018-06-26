using System;

namespace ExportToService.Dto
{
    /// <summary>
    /// Информация о транзакции
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// Номер телефона
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// ID транзакции
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Номер карты
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// ID бонусной карты
        /// </summary>
        public string CardId { get; set; }

        /// <summary>
        /// Сумма транзакции
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Остаток на карте до выполнения транзакции
        /// </summary>
        public decimal BalanceOnTransaction { get; set; }

        /// <summary>
        /// Тип транзакции
        /// </summary>
        public TypeTransaction TypeTransaction { get; set; }

        /// <summary>
        /// Дата и время транзакции
        /// </summary>
        public DateTime TransactionDateTime { get; set; }
    }

    /// <summary>
    /// Тип транзакции {7 = Начисление; 2 = Списание}
    /// </summary>
    public enum TypeTransaction
    {
        InCard = 7,
        OutCard = 2,
        AddToCard = 0
    }
}