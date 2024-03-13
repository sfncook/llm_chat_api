namespace SalesBotApi.Models

{
    public class UserWithJwt : UserBase
    {
        public string jwt { get; set; }

        public UserWithJwt() {
        }

        public UserWithJwt(UserWithPassword userWithPassword) {
            id = userWithPassword.id;
            user_name = userWithPassword.user_name;
            company_id = userWithPassword.company_id;
            role = userWithPassword.role;
            approval_status = userWithPassword.approval_status;
        }
    }
}