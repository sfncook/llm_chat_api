using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using static LogBufferService;
using System.Collections.Generic;
using System;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class LogsController : Controller
    {

        private readonly Container logsContainer;

        public LogsController(CosmosDbService cosmosDbService)
        {
            logsContainer = cosmosDbService.LogsContainer;
        }

        // GET: api/logs
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<List<LogMsg>>> GetLogs(
            [FromQuery] string level,
            [FromQuery] int offset,
            [FromQuery] int limit
        ) {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }

            if(offset == 0 && limit ==0) {
                return BadRequest();
            }

            string sqlQueryText = $"SELECT * FROM c ORDER BY c.time DESC OFFSET {offset} LIMIT {limit}";
            if(level != null) {
                sqlQueryText = $"SELECT * FROM c WHERE c.levelStr = '{FirstCharToUpper(level)}' ORDER BY c.time DESC OFFSET {offset} LIMIT {limit}";
            }
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<LogMsg> logMsgs = new List<LogMsg>();
            using (FeedIterator<LogMsg> feedIterator = logsContainer.GetItemQueryIterator<LogMsg>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<LogMsg> response = await feedIterator.ReadNextAsync();
                    logMsgs.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return logMsgs;
        }

        // GET: api/logs/count
        [HttpGet("count")]
        [JwtAuthorize]
        public async Task<ActionResult<long>> GetLogsManyTotal()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }
            string sqlQueryText = $"SELECT count(m) as many_msgs FROM m";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            using (FeedIterator<MessagesManyPerConvo> feedIterator = logsContainer.GetItemQueryIterator<MessagesManyPerConvo>(queryDefinition))
            {
                if (feedIterator.HasMoreResults)
                {
                    FeedResponse<MessagesManyPerConvo> response = await feedIterator.ReadNextAsync();
                    return response.First().many_msgs;
                }
            }
            return 0;
        }

        private string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            string _input = input.ToLower();
            return $"{char.ToUpper(_input[0])}{input[1..]}";
        }
    }
}
