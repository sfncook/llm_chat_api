using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using static MetricsBufferService;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class MetricsController : Controller
    {

        private readonly Container metricsContainer;

        public MetricsController(CosmosDbService cosmosDbService)
        {
            metricsContainer = cosmosDbService.MetricsContainer;
        }

        // GET: api/metrics
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<List<MetricEvent>>> GetMetrics(
            [FromQuery] string event_id,
            [FromQuery] long since_timestamp_sec
        ) {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }

            string sqlQueryText = $"SELECT * FROM c WHERE c._ts >= {since_timestamp_sec} AND c.event_id = '{event_id}' ORDER BY c._ts DESC";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<MetricEvent> events = new List<MetricEvent>();
            using (FeedIterator<MetricEvent> feedIterator = metricsContainer.GetItemQueryIterator<MetricEvent>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<MetricEvent> response = await feedIterator.ReadNextAsync();
                    events.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return events;
        }
    }
}
