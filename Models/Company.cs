namespace SalesBotApi.Models

{
    public class Company
    {
        public string id { get; set; }
        public string company_id { get; set; }
        public long _ts { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool training { get; set; }
        public string email_for_leads { get; set; }
        public string hubspot_access_token { get; set; }
    }
}