﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common;

namespace IntelligenceHub.Client
{
    public class FunctionClient
    {
        private HttpClient _client { get; set; }
        public FunctionClient(IHttpClientFactory clientFactory) 
        {
            _client = clientFactory.CreateClient(GlobalVariables.ClientPolicy.FunctionClient.ToString());
        }

        public async Task<HttpResponseMessage> CallFunction(string toolName, string toolArgs, string endpoint, string? httpMethod = "Post", string? key = null)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(key)) _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", key);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var json = string.Empty;
            if (!string.IsNullOrEmpty(toolArgs))
            {
                var tool = new ToolExecutionCall()
                {
                    ToolName = toolName,
                    Arguments = toolArgs,   
                };
                json = JsonConvert.SerializeObject(tool, settings);
            }
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                if (httpMethod == HttpMethod.Post.ToString()) return await _client.PostAsync(endpoint, body);
                else if (httpMethod == HttpMethod.Get.ToString()) return await _client.GetAsync(endpoint);
                else if (httpMethod == HttpMethod.Put.ToString()) return await _client.PutAsync(endpoint, body);
                else if (httpMethod == HttpMethod.Patch.ToString()) return await _client.PatchAsync(endpoint, body);
                else if (httpMethod == HttpMethod.Delete.ToString()) return await _client.DeleteAsync(endpoint);
                else return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }
            catch (HttpRequestException ex)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { ReasonPhrase = ex.Message };
            }
            catch (TaskCanceledException ex)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout) { ReasonPhrase = ex.Message };
            }
        }
    }
}
