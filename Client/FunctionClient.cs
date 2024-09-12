using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace IntelligenceHub.Client
{
    public class FunctionClient
    {
        private HttpClient _client { get; set; }
        private string _endpoint;
        public FunctionClient(IHttpClientFactory clientFactory) 
        {
            _client = clientFactory.CreateClient("FunctionCalling"); implement FunctionCalling configuration
        }

        public async Task<HttpResponseMessage> CallFunction(ResponseToolDTO tool, string endpoint)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(tool, settings);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            using (_client)
            {
                try
                {
                    var response = await _client.PostAsync(_endpoint, body);
                    //response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}
