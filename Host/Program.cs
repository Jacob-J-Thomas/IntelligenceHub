using IntelligenceHub.Business;
using IntelligenceHub.Client;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Hubs;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IntelligenceHub.Host.Config;
using Microsoft.OpenApi.Models;
using Microsoft.ApplicationInsights.AspNetCore.Logging;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Azure;
using IntelligenceHub.Host.Policies;
using Polly.Wrap;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Host.Logging;

namespace IntelligenceHub.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var agiClientSettings = builder.Configuration.GetRequiredSection(nameof(AGIClientSettings)).Get<AGIClientSettings>();
            var insightSettings = builder.Configuration.GetRequiredSection(nameof(AppInsightSettings)).Get<AppInsightSettings>();

            // Add Services
            builder.Services.AddSingleton(agiClientSettings);
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(Settings)).Get<Settings>());
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(SearchServiceClientSettings)).Get<SearchServiceClientSettings>());
            builder.Services.AddSingleton<IAGIClient, AGIClient>();
            builder.Services.AddSingleton<IAISearchServiceClient, AISearchServiceClient>();
            builder.Services.AddSingleton<ICompletionLogic, CompletionLogic>();
            builder.Services.AddSingleton(new LoadBalancingSelector(agiClientSettings.Services.Select(service => service.Endpoint).ToArray()));

            #region Configure Client Policies
            // Function Calling Client Policies:

            // Define the FunctionClient policy
            builder.Services.AddHttpClient(ClientPolicy.FunctionClient.ToString()).AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // AGI Completion Policies

            // Define the Completion retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10));

            // Define the Completion circuit breaker policy if more than one service exists
            IAsyncPolicy<HttpResponseMessage> policyWrap = retryPolicy;
            if (agiClientSettings.Services.Count > 1)
            {
                var circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,  // Break after 3 consecutive failures.
                    durationOfBreak: TimeSpan.FromSeconds(30), // Open the circuit for 30 seconds.
                    onBreak: (result, breakDelay) =>
                    {
                        Console.WriteLine($"Circuit opened for {breakDelay.TotalSeconds} seconds.");
                    },
                    onReset: () => Console.WriteLine("Circuit closed - normal operation resumed."),
                    onHalfOpen: () => Console.WriteLine("Circuit is half-open - trial requests allowed.")
                );

                // Combine the Completion retry and circuit breaker policies.
                policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
            }

            // Register the HttpClient with the load balancing handler and policy.
            builder.Services.AddHttpClient(ClientPolicy.CompletionClient.ToString(), (serviceProvider, client) =>
            {
                // Get the BaseAddressSelector from the service provider.
                var baseAddressSelector = serviceProvider.GetRequiredService<LoadBalancingSelector>();

                // Set the HttpClient's BaseAddress to the next available backend URL.
                client.BaseAddress = baseAddressSelector.GetNextBaseAddress();
                Console.WriteLine($"Configured HttpClient with BaseAddress: {client.BaseAddress}");
            }).AddPolicyHandler(policyWrap);
            #endregion

            #region Json Serialization Settings

            // Configure json serialization for controllers and hubs
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
            #endregion

            #region Logging

            // Add Logging via Application Insights
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = insightSettings?.ConnectionString;
            });

            builder.Services.AddLogging(options =>
            {
                options.AddApplicationInsights();
            });
            #endregion

            #region Authentication

            // Configure Auth
            var authSettings = builder.Configuration.GetRequiredSection(nameof(AuthSettings)).Get<AuthSettings>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = authSettings.Domain;
                options.Audience = authSettings.Audience;
            });

            // Configure swagger to generate auth tokens
            builder.Services.AddSwaggerGen(options =>
            {
                // Define the security scheme
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by a space and the JWT token."
                });

                // Apply the security scheme globally to all operations
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            #endregion

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

            app.UseMiddleware<LoggingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream");

            app.Run();
        }
    }
}