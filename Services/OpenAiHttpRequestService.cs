using System.Threading.Tasks;
using SalesBotApi.Models;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Options;

public class OpenAiHttpRequestService
{

    private readonly HttpClient _httpClient;
    private readonly LogBufferService logger;
    private readonly MetricsBufferService metrics;
    private readonly string openaiApikey;
    public OpenAiHttpRequestService(
        LogBufferService logger,
        MetricsBufferService metrics,
        IOptions<MyConnectionStrings> myConnectionStrings
    )
    {
        _httpClient = new HttpClient();
        this.logger = logger;
        this.metrics = metrics;
        openaiApikey = myConnectionStrings.Value.OpenAiApiKey;
    }

    public async Task<AssistantResponse> SubmitUserQuestion(
        string userQuestion, 
        Company company,
        Conversation convo,
        IEnumerable<Refinement> refinements,
        IEnumerable<Message> messages
    )
    {
        PromptBuilder promptBuilder = new PromptBuilder();
        string prompt = promptBuilder
            .setUserQuestion(userQuestion)
            .setMessages(messages)
            .setRefinements(refinements)
            .build();

        OpenAiRequestBuilder openAiRequestBuilder = new OpenAiRequestBuilder();
        string reqParams = openAiRequestBuilder
            .setModel("gpt-4-0125-preview") // gpt-4-0125-preview gpt-3.5-turbo-1106
            .setUserQuestion(userQuestion)
            .setSystemPrompt(prompt)
            .setMessages(messages)
            .build();

        reqParams = EscapeStringForJson(reqParams);

        var content = new StringContent(reqParams, Encoding.UTF8, "application/json");

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("Authorization", $"Bearer {openaiApikey}");

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            ChatCompletionResponse chatCompletionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseString);
            string argumentsStr = chatCompletionResponse.choices[0].message.tool_calls[0].function.arguments;
            metrics.Count("openai.usage.prompt_tokens", chatCompletionResponse.usage.prompt_tokens, tag:company.company_id);
            metrics.Count("openai.usage.completion_tokens", chatCompletionResponse.usage.completion_tokens, tag:company.company_id);
            metrics.Count("openai.usage.total_tokens", chatCompletionResponse.usage.total_tokens, tag:company.company_id);

            AssistantResponse assistantResponse;
            // Sometime we're getting JSON failure parsion the function arguments, so I'm assuming the LLM doesn't always call the function and sometimes it
            //  screws up and just returns a string (message.content).  Hence this try-catch block.
            try {
                assistantResponse = JsonConvert.DeserializeObject<AssistantResponse>(argumentsStr);
            } catch(JsonReaderException) {
                string messageContent = chatCompletionResponse.choices[0].message.content;
                if(messageContent!=null){
                    metrics.Inc("opeanai.json_exception.message_content_not_null");
                    logger.Error($"JSON Exception trying to parse assistant response argumentsStr:{argumentsStr} but message.content was NON NULL:{messageContent}");
                    assistantResponse = new AssistantResponse {
                        assistant_response = messageContent
                    };
                } else {
                    metrics.Inc("opeanai.json_exception.message_content_is_null");
                    logger.Error($"JSON Exception trying to parse assistant response argumentsStr:{argumentsStr} and message.content was NULL so throwing the exception :(");
                    throw;
                }
            }
            return assistantResponse;
        }
    }

    public class FunctionDef {
        public string name { get; set;}
        public string description { get; set;}
        public AssistantResponse parameters { get; set;}
    }

    public class ChatCompletionResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
        public object system_fingerprint { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public _Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class _Message
    {
        public string role { get; set; }
        public string content { get; set; }
        public List<ToolCall> tool_calls { get; set; }
    }

    public class ToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public Function function { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public string arguments { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public static string EscapeStringForJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ")
                    .Replace("\b", " ")
                    .Replace("\f", " ")
                    .Replace("  ", " ")
                    .Replace("'", "")
                    ;
    }

}

