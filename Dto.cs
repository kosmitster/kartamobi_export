using System;

namespace ExportToService
{
    public class Dto
    {
        public string PhoneNumber { get; set; }
        public string TransactionId { get; set; }
        public string CardNumber { get; set; }
        public string OrderId { get; set; }
        public string CardId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public TypeBonus TypeBonus { get; set; }
        public DateTime TransactionDateTime { get; set; }
    }
    
    public enum TypeBonus { InCard = 7, OutCard = 2 }
}
