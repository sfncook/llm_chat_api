namespace SalesBotApi.Models
{
    public class Refinement
    {
        public string id { get; set; }
        public string company_id { get; set; }
        public string conversation_id { get; set; }
        public string message_id { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public bool is_positive { get; set; }
    }
}