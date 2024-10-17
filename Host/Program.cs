using IntelligenceHub.Business;
using IntelligenceHub.Client;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Hubs;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json.Serialization;

namespace IntelligenceHub.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton(builder.Configuration.GetSection("Settings").Get<Settings>());
            builder.Services.AddSingleton(builder.Configuration.GetSection("AIClientSettings").Get<AGIClientSettings>());
            builder.Services.AddSingleton(builder.Configuration.GetSection("SearchServiceSettings").Get<SearchServiceClientSettings>());
            builder.Services.AddSingleton<IAGIClient, AGIClient>();
            builder.Services.AddSingleton<IAISearchServiceClient, AISearchServiceClient>();
            builder.Services.AddSingleton<ICompletionLogic, CompletionLogic>();

            // Configure HttpClient with Polly retry policy
            builder.Services.AddHttpClient("FunctionClient").AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            builder.Services.AddSignalR().AddJsonProtocol(options => 
            {
                // Set serialization for global enums utilized in DTOs
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                // Set serialization for global enums utilized in DTOs
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                    policy.SetIsOriginAllowed((host) => true);
                });
            }
            else
            {
                // configure prod cors policy
            }

            app.UseFileServer();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chatstream");
                endpoints.MapControllers();
            });

            app.Run();
        }
    }
}