namespace SalesBotApi.Models

{
    public class EmailRequest
    {
        public string sender_email { get; set; }
        public string sender_name { get; set; }
        public string recipient_email { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
    }
}