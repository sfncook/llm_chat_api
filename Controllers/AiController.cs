using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Diagnostics;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {
        private readonly OpenAiHttpRequestService openAiHttpRequestService;
        private readonly SharedQueriesService sharedQueriesService;
        private readonly Container messagesContainer;
        private readonly LogBufferService logger;
        private readonly MetricsBufferService metrics;

        public AiController(
            OpenAiHttpRequestService _openAiHttpRequestService,
            SharedQueriesService _sharedQueriesService,
            CosmosDbService cosmosDbService,
            LogBufferService logger,
            MetricsBufferService metricsBufferService
        )
        {
            openAiHttpRequestService = _openAiHttpRequestService;
            sharedQueriesService = _sharedQueriesService;
            messagesContainer = cosmosDbService.MessagesContainer;
            this.logger = logger;
            this.metrics = metricsBufferService;
        }

        // PUT: api/ai/submit_user_question
        [HttpPut("submit_user_question")]
        public async Task<ActionResult<SubmitResponse>> SubmitUserQuestion(
            [FromBody] SubmitRequest req,
            [FromQuery] string companyid,
            [FromQuery] string convoid
        )
        {
            Stopwatch stopwatch1 = Stopwatch.StartNew();
            if(req==null || req.user_msg==null){
                return BadRequest();
            }

            Console.WriteLine($"METRICS *** START ***");

            Company company = null;
            Conversation convo = null;
            IEnumerable<Message> messages = null;
            IEnumerable<Refinement> refinements = null;

            Stopwatch stopwatch = Stopwatch.StartNew();
            Task<IEnumerable<Message>> msgsTask;
            try
            {
                // Message cannot be effectively cached so I'm pulling this out
                msgsTask = sharedQueriesService.GetRecentMsgsForConvo(convoid, 4);
                var companyTask = sharedQueriesService.GetCompanyById(companyid);
                var convoTask = sharedQueriesService.GetConversationById(convoid);
                var refinementsTask = sharedQueriesService.GetRefinementsByCompanyId(companyid);

                await Task.WhenAll(
                    companyTask, 
                    convoTask, 
                    refinementsTask
                );

                // After all tasks are complete, you can assign the results
                company = await companyTask;
                convo = await convoTask;
                refinements = await refinementsTask;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            stopwatch.Stop();
            Console.WriteLine($"METRICS (COSMOS & OpenAI-Embeddings) Load cosmos data: {stopwatch.ElapsedMilliseconds} ms");
            metrics.Duration("submit_user_question.load_cosmos_data.ms", stopwatch.ElapsedMilliseconds);


            messages = await msgsTask;

            AssistantResponse assistantResponse = await SubmitUserQuestionToAi(
                req.user_msg, 
                company, 
                convo, 
                refinements,
                messages
            );

            stopwatch = Stopwatch.StartNew();
            
            await InsertNewMessage(
                convo,
                req.user_msg,
                assistantResponse
            );
            
            stopwatch.Stop();
            Console.WriteLine($"METRICS insert-msg: {stopwatch.ElapsedMilliseconds} ms");
            metrics.Duration("submit_user_question.insert_msg.ms", stopwatch.ElapsedMilliseconds);

            SubmitResponse submitResponse = new SubmitResponse() {
                assistant_response = new SubmitResponseAssistantResponse(){role="assistant", content=assistantResponse.assistant_response},
            };

            Console.WriteLine($"METRICS *** END ***\n");
            stopwatch1.Stop();
            Console.WriteLine($"METRICS (Full) SubmitUserMessage: {stopwatch1.ElapsedMilliseconds} ms");
            metrics.Duration("submit_user_question.func_duration.ms", stopwatch1.ElapsedMilliseconds);

            return Ok(submitResponse);
        }

        private async Task<AssistantResponse> SubmitUserQuestionToAi(
            string user_msg, 
            Company company,
            Conversation convo,
            IEnumerable<Refinement> refinements,
            IEnumerable<Message> messages
        )
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            AssistantResponse assistantResponse = await openAiHttpRequestService.SubmitUserQuestion(
                user_msg, 
                company, 
                convo, 
                refinements,
                messages
            );
            stopwatch.Stop();
            Console.WriteLine($"METRICS (OpenAI) Get chat assistant response: {stopwatch.ElapsedMilliseconds} ms");
            metrics.Duration("openai_llm_request.ms", stopwatch.ElapsedMilliseconds);
            return assistantResponse;
        }

        private async Task InsertNewMessage(
            Conversation convo,
            string user_msg,
            AssistantResponse assistantResponse
        ) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Message message = new Message {
                id = Guid.NewGuid().ToString(),
                conversation_id = convo.id,
                user_msg = user_msg,
                assistant_response = assistantResponse,
                company_id = convo.company_id
            };
            await messagesContainer.CreateItemAsync(message, new PartitionKey(convo.id));
            stopwatch.Stop();
            Console.WriteLine($"--> METRICS (Cosmos) Insert new message: {stopwatch.ElapsedMilliseconds} ms");
            metrics.Duration("cosmos_insert_new_msg.ms", stopwatch.ElapsedMilliseconds);
        }

    }//class AiController
    
    public class SubmitResponse {
        public SubmitResponseAssistantResponse assistant_response { get; set; }
        public string redirect_url { get; set; }
    }
    public class SubmitResponseAssistantResponse {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class SubmitRequest {
        public string user_msg { get; set; }
        public bool mute { get; set; }
    }

    public class GptMessage {
        public string role { get; set; }
        public string content { get; set; }
    }
}
