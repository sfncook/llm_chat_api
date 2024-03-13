namespace SalesBotApi.Models

{
    public class NewCompanyRequest
    {
        public string name { get; set; }
        public string description { get; set; }
        public string email_for_leads { get; set; }
    }
}