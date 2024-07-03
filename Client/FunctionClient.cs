using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class FunctionClient
    {
        private string _endpoint;
        public FunctionClient(string endpoint) 
        {
            _endpoint = endpoint;
        }

        public async Task<HttpResponseMessage> CallFunction(ResponseToolDTO tool)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(tool, settings);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            using (client)
            {
                try
                {
                    var response = await client.PostAsync(_endpoint, body);
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
