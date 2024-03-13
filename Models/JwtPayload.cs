namespace SalesBotApi.Models

{
    public class JwtPayload : UserBase
    {
        public long exp { get; set; }
    }
}