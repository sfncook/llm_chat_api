using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;

namespace SalesBotApi.Controllers
{
    public class ApprovalStatusResponse {
        public string approval_status { get; set; }
    }
    public class UpdateUserStatusRequest {
        public UserBase user { get; set; }
        public string approval_status { get; set; }
    }

    [Route("api/[controller]")] 
    [ApiController]
    public class UsersController : Controller
    {

        private readonly Container usersContainer;
        private readonly EmailService emailService;

        public UsersController(CosmosDbService cosmosDbService, EmailService _emailService)
        {
            usersContainer = cosmosDbService.UsersContainer;
            emailService = _emailService;
        }

        // GET: api/users/approval_status
        [HttpGet("approval_status")]
        [JwtAuthorize]
        public async Task<ActionResult<ApprovalStatusResponse>> GetUserApprovalStatus()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string user_id = userData.id;
            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{user_id}'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            UserBase user = null;
            try
            {
                using (FeedIterator<UserBase> feedIterator = usersContainer.GetItemQueryIterator<UserBase>(queryDefinition))
                {
                    if (feedIterator.HasMoreResults)
                    {
                        FeedResponse<UserBase> response = await feedIterator.ReadNextAsync();
                        user = response.First();
                    }
                }
            }
            catch (CosmosException)
            {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            if(user != null) {
                ApprovalStatusResponse resp = new ApprovalStatusResponse
                {
                    approval_status = user.approval_status
                };
                return Ok(resp);
            }
            else return Unauthorized();
        }

        // *** ROOT ADMIN ***
        // GET: api/users
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<UserBase>>> GetAllUsers()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if(userData.role != "root") {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            string sqlQueryText = "SELECT * FROM c";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<UserBase> users = new List<UserBase>();
            using (FeedIterator<UserBase> feedIterator = usersContainer.GetItemQueryIterator<UserBase>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<UserBase> response = await feedIterator.ReadNextAsync();
                    users.AddRange(response.ToList());
                }
            }
            return users;
        }

        // PUT: api/users/approval_status
        [HttpPut("approval_status")]
        [JwtAuthorize]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest req)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if(userData.role != "root") {
                return Unauthorized();
            }

            if(req.user == null || req.approval_status == null)
            {
                return BadRequest("Missing request data");
            }

            try
            {
                List<PatchOperation> patchOperations = new List<PatchOperation>()
                {
                    PatchOperation.Replace("/approval_status", req.approval_status)
                };
                await usersContainer.PatchItemAsync<dynamic>(req.user.id, new PartitionKey(req.user.company_id), patchOperations);

                // if(req.approval_status=="approved") {
                //     await emailService.SendRegistrationApprovalEmail(req.user.user_name);
                // } else {
                //     await emailService.SendRegistrationDeniedEmail(req.user.user_name);
                // }

                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
