namespace KartaMobiExporter.Dto
{
    /// <summary>
    /// Тип транзакции {7 = Начисление; 2 = Списание}
    /// </summary>
    public enum TransactionType
    {
        InCard = 7,
        OutCard = 2,
        AddToCard = 0
    }
}
