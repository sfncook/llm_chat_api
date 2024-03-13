namespace SalesBotApi.Models

{
    public class Message
    {
        public string id { get; set; }
        public long _ts { get; set; }
        public string conversation_id { get; set; }
        public string company_id { get; set; }
        public string user_msg { get; set; }
        public AssistantResponse assistant_response { get; set; }
    }
}
