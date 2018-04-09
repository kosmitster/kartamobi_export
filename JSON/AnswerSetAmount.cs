namespace ExportToService.JSON
{
    public partial class Data
    {
        public string card_num { get; set; }
        public string bonuses { get; set; }
        public string writeoff { get; set; }
    }

    public class AnswerSetAmount
    {
        public bool status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
}
