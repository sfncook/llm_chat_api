namespace TodoApi.Models

{
    public class Message
    {
        public string id { get; set; }
        public long _ts { get; set; }
        public string conversation_id { get; set; }
        public string user_msg { get; set; }
        public string assistant_response { get; set; }
    }
}