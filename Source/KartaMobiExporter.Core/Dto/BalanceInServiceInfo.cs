namespace KartaMobiExporter.Core.Dto
{
    /// <summary>
    /// Информация о балансе Karta.Mobi
    /// </summary>
    public class BalanceInServiceInfo
    {
        /// <summary>
        /// Уникальный идентификатор карты
        /// </summary>
        public string CardId { get; set; }

        /// <summary>
        /// Номер телефона
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Баланс карты Karta.Mobi
        /// </summary>
        public decimal Bonuses { get; set; }

        /// <summary>
        /// Остаток на карте на момент выполенения
        /// </summary>
        public decimal BalanceOnRealTime { get; set; }

        /// <summary>
        /// Токен клиента
        /// </summary>
        public string UToken { get; set; } 
    }
}