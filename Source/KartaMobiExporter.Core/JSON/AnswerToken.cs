﻿namespace KartaMobiExporter.Core.JSON
{

    public partial class Data
    {
        public string u_token { get; set; }
    }

    public class AnswerToken
    {
        public bool status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }

}