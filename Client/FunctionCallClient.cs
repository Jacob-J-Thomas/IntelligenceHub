﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;
using OpenAICustomFunctionCallingAPI.Client.DTOs.FunctionCalling;

namespace OpenAICustomFunctionCallingAPI.Client
{
    public class FunctionCallClient
    {
        private string _endpoint;
        public FunctionCallClient(string endpoint) 
        {
            _endpoint = endpoint;
        }

        public async Task CallFunction(string prompt, string functionName)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new OutgoingFunctionCall(prompt, functionName);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(request, settings);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            using (client)
            {
                try
                {
                    var response = await client.PostAsync(_endpoint, body);
                    response.EnsureSuccessStatusCode();
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}
