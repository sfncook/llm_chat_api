using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SalesBotApi.Controllers;
using SalesBotApi.Models;

public class OpenAiRequestBuilder
{

    private string system_prompt;
    private string user_question;
    private string model;
    private IEnumerable<Message> messages;

    public static string reqParamsTemplate;

    public OpenAiRequestBuilder setSystemPrompt(string system_prompt){
        this.system_prompt = system_prompt;
        return this;
    }
    public OpenAiRequestBuilder setUserQuestion(string user_question){
        this.user_question = user_question;
        return this;
    }
    public OpenAiRequestBuilder setModel(string model){
        this.model = model;
        return this;
    }
    public OpenAiRequestBuilder setMessages(IEnumerable<Message> messages){
        this.messages = messages;
        return this;
    }

    public string build() {
        string reqParams = reqParamsTemplate;
        reqParams = setAllMessagesInTemplate(reqParams);
        reqParams = replaceInTemplate(reqParams, "model", model);
        return reqParams;
    }

    private string replaceInTemplate(string prompt, string key, string value) {
        return prompt.Replace("{"+key+"}", value);
    }

    private string setAllMessagesInTemplate(string reqParams) {
        List<GptMessage> allMsgs = new List<GptMessage>();

        allMsgs.Add(new GptMessage{
            role = "system", 
            content = system_prompt
        });

        var sortedMessages = messages.OrderBy(message => message._ts);
        foreach(Message msg in sortedMessages) {
            if(msg.user_msg!=null) {
                allMsgs.Add(new GptMessage{
                    role = "user", 
                    content = msg.user_msg
                });
            }
            if(msg.assistant_response != null && msg.assistant_response.assistant_response != null) {
                allMsgs.Add(new GptMessage{
                    role = "assistant", 
                    content = msg.assistant_response.assistant_response
                });
            }
        }

        allMsgs.Add(new GptMessage{
            role = "user", 
            content = user_question
        });


        string allMsgsStr = JsonConvert.SerializeObject(allMsgs.ToArray());
        return reqParams.Replace("\"{messages}\"", allMsgsStr);
    }

    public static string EscapeStringForJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("\"", "\\\"")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ")
                    .Replace("\b", " ")
                    .Replace("\f", " ");
    }

    public static async Task LoadOpenAiRequestContentJson()
    {
        string resourceName = "openai_request_content.json";
        // Get the current assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Combine the assembly's name and the resource name
        var resourcePath = assembly.GetName().Name + "." + resourceName.Replace("/", ".");

        // Find the resource stream
        using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
        {
            if (stream == null)
            {
                throw new FileNotFoundException($"Resource '{resourceName}' not found.");
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                reqParamsTemplate = await reader.ReadToEndAsync();
            }
        }
    }
}