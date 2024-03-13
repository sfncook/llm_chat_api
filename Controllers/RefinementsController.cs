using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class RefinementsController : Controller
    {
        public class AddRefinementRequest {
            public string message_id { get; set; }
            public string convo_id { get; set; }
            public string question { get; set; }
            public string answer { get; set; }
            public bool is_positive { get; set; }
        }
        
        private readonly Container refinementsContainer;
        private readonly SharedQueriesService sharedQueriesService;
        private readonly ICacheProvider<IEnumerable<Refinement>> cacheRefinements;

        public RefinementsController(
            CosmosDbService cosmosDbService, 
            SharedQueriesService _sharedQueriesService,
            InMemoryCacheService<IEnumerable<Refinement>> cacheRefinements
        )
        {
            refinementsContainer = cosmosDbService.RefinementsContainer;
            sharedQueriesService = _sharedQueriesService;
            this.cacheRefinements = cacheRefinements;
        }

        // GET: api/refinements
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Refinement>>> GetRefinements()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            IEnumerable<Refinement> refinements = await sharedQueriesService.GetRefinementsByCompanyId(company_id);
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(refinements);
        }

        // PUT: api/refinements
        [HttpPut]
        [JwtAuthorize]
        public async Task<IActionResult> UpdateRefinement([FromBody] Refinement refinement)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            if(company_id!="all" && company_id!=refinement.company_id) {
                return Unauthorized();
            }

            try
            {
                await refinementsContainer.ReplaceItemAsync(refinement, refinement.id);
                cacheRefinements.Clear(refinement.company_id);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }


        // POST: api/refinements
        [HttpPost]
        [JwtAuthorize]
        public async Task<IActionResult> AddRefinement([FromBody] AddRefinementRequest req)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            if(req.message_id == null || req.convo_id == null) {
                return BadRequest();
            }
            
            Message msg;
            try {
                msg = await sharedQueriesService.GetMessageById(req.message_id, req.convo_id);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Message not found");
            }

            if(company_id != "all" && msg.company_id != company_id){
                return Unauthorized();
            }

            Refinement refinement = new Refinement
            {
                id = Guid.NewGuid().ToString(),
                company_id = msg.company_id,
                conversation_id = msg.conversation_id,
                message_id = msg.id,
                question = req.question,
                answer = req.answer,
                is_positive = req.is_positive
            };
            
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await refinementsContainer.CreateItemAsync(refinement, new PartitionKey(company_id));
            cacheRefinements.Clear(msg.company_id);
            return NoContent();
        }
    }
}
