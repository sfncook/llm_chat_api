namespace SalesBotApi.Models

{
    public class UserWithPassword : UserBase
    {
        public string password { get; set; }
        public byte[] salt { get; set; }
    }
}