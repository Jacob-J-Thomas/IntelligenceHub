using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenAICustomFunctionCallingAPI.Client.DTOs.OpenAI;
using OpenAICustomFunctionCallingAPI.Client.OpenAI.DTOs;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class AIClient
    {
        private string _openAIEndpoint;
        private string _openAIKey;
        private string _openAIModel;

        public AIClient(string openAIEndpoint, string openAIKey, string openAIModel) 
        {
            _openAIEndpoint = openAIEndpoint;
            _openAIKey = openAIKey;
            _openAIModel = openAIModel;
        }

        public async Task<JObject> Post(string prompt, string instructions, object toolFunction)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            OpenAIRequest request = new OpenAIRequest();
            request.Model = _openAIModel;
            request.Tools = new List<Tool>()
            {
                new Tool(toolFunction)
            };
            request.Messages.Add(new Message("system", instructions));
            request.Messages.Add(new Message("user", prompt));

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(request, settings);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            using (client)
            {
                try
                {
                    var response = await client.PostAsync(_openAIEndpoint, body);
                    response.EnsureSuccessStatusCode();

                    var responseString = await response.Content.ReadAsStringAsync();
                    var completionResponse = JObject.Parse(responseString);
                    return completionResponse;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}
