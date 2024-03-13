using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;

namespace SalesBotApi.Controllers
{

    [Route("api/[controller]")] 
    [ApiController]
    public class RegistrationController : Controller
    {

        private readonly Container usersContainer;
        private readonly SharedQueriesService queriesSvc;
        private readonly EmailService emailService;
        private readonly PasswordHasherService passwordHasherService;

        public RegistrationController(
            CosmosDbService cosmosDbService,
            SharedQueriesService _queriesSvc,
            EmailService _emailService,
            PasswordHasherService passwordHasherService
        )
        {
            usersContainer = cosmosDbService.UsersContainer;
            queriesSvc = _queriesSvc;
            emailService = _emailService;
            this.passwordHasherService = passwordHasherService;
        }

        // POST: api/register
        [HttpPost]
        public async Task<IActionResult> RegisterNewUser([FromBody] LoginRequest loginReq)
        {
            if (loginReq.user_name == null || loginReq.password == null)
            {
                return BadRequest("Invalid request, missing parameters");
            }

            IEnumerable<UserWithJwt> users = await queriesSvc.GetAllItems<UserWithJwt>(usersContainer);
            foreach (UserWithJwt preexistingUser in users)
            {
                if (preexistingUser.user_name.ToLower() == loginReq.user_name.ToLower())
                {
                    return BadRequest("User exists");
                }
            }

            var (hashedPassword, salt) = passwordHasherService.HashPassword(loginReq.password);
            string newUuid = Guid.NewGuid().ToString();
            string companyId = "XXX";
            UserWithPassword newUser = new UserWithPassword
            {
                id = newUuid,
                user_name = loginReq.user_name,
                password = hashedPassword,
                salt = salt,
                company_id = companyId,
                role = "company_owner",
                approval_status = ""
            };

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var newUserTask = usersContainer.CreateItemAsync(newUser, new PartitionKey(companyId));
            // var regEmailTask = emailService.SendRegistrationEmail(loginReq.user_name);
            // var newRegAdminEmailTask = emailService.SendNewRegistrationAdminEmail();

            // await Task.WhenAll(newUserTask, regEmailTask, newRegAdminEmailTask);
            await newUserTask;
            // await regEmailTask;
            // await newRegAdminEmailTask;

            return NoContent();
        }
    }
}
