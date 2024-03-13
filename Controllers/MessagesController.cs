using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class MessagesController : Controller
    {
        private readonly Container messagesContainer;

        public MessagesController(CosmosDbService cosmosDbService)
        {
            messagesContainer = cosmosDbService.MessagesContainer;
        }

        // GET: api/messages
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages(
            [FromQuery] string convo_id,
            [FromQuery] bool? latest
        )
        
        {
            if (convo_id != null && latest != null)
            {
                return BadRequest($"Invalid or missing parameters");
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            string sqlQueryText = "";
            if (convo_id != null) {
                sqlQueryText = $"SELECT * FROM m WHERE m.conversation_id = '{convo_id}' ";
                if (company_id != "all") {
                    sqlQueryText += $" AND m.company_id = '{company_id}'";
                }
                sqlQueryText += $" ORDER BY m.timestamp ASC";
            }
            if (latest != null) {
                sqlQueryText = "SELECT  * FROM c WHERE c._ts >= (GetCurrentTimestamp() / 1000) - (30 * 24 * 60 * 60) AND is_string(c.company_id)";
                if (company_id != "all") {
                    sqlQueryText += $" AND c.company_id = '{company_id}'";
                }
            }
            
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Message> messages = new List<Message>();
            using (FeedIterator<Message> feedIterator = messagesContainer.GetItemQueryIterator<Message>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Message> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return messages;
        }

        // GET: api/messages/count_per_convo
        [HttpGet("count_per_convo")]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<MessagesManyPerConvo>>> GetMessageCounts()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            string sqlQueryText = $"SELECT count(m) as many_msgs, m.conversation_id FROM m";
            if(company_id != "all") {
                sqlQueryText += $" WHERE m.company_id = '{company_id}'";
            }
            sqlQueryText += " GROUP BY m.conversation_id";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<MessagesManyPerConvo> messages = new List<MessagesManyPerConvo>();
            using (FeedIterator<MessagesManyPerConvo> feedIterator = messagesContainer.GetItemQueryIterator<MessagesManyPerConvo>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<MessagesManyPerConvo> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return messages;
        }
    }
}
