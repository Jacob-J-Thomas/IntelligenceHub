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
using IntelligenceHub.Host.Policies;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Host.Logging;
using Microsoft.IdentityModel.Tokens;
using IntelligenceHub.Common.Handlers;

namespace IntelligenceHub.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Add Services and Settings
            var agiClientSettings = builder.Configuration.GetRequiredSection(nameof(AGIClientSettings)).Get<AGIClientSettings>();
            var insightSettings = builder.Configuration.GetRequiredSection(nameof(AppInsightSettings)).Get<AppInsightSettings>();

            // Add Services
            builder.Services.AddSingleton(agiClientSettings);
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(Settings)).Get<Settings>());
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(SearchServiceClientSettings)).Get<SearchServiceClientSettings>());
            builder.Services.AddSingleton<IAGIClient, AGIClient>();
            builder.Services.AddSingleton<IAISearchServiceClient, AISearchServiceClient>();
            builder.Services.AddSingleton<ICompletionLogic, CompletionLogic>();
            builder.Services.AddSingleton<IMessageHistoryLogic, MessageHistoryLogic>();
            builder.Services.AddSingleton<IProfileLogic, ProfileLogic>();
            builder.Services.AddSingleton<IRagLogic, RagLogic>();
            builder.Services.AddSingleton<IValidationHandler, ValidationHandler>();
            builder.Services.AddSingleton(new LoadBalancingSelector(agiClientSettings.Services.Select(service => service.Endpoint).ToArray()));
            #endregion

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
                .WaitAndRetryAsync(5, _ =>
                {
                    // Random jitter up to 5 seconds
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 5000)); 
                    return TimeSpan.FromSeconds(10) + jitter;
                });

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

                // Specify the Role Claim Type if necessary
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "roles"
                };
            });

            // Add role-based authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy => 
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => (c.Type == "scope" || c.Type == "permissions") && c.Value.Split(' ').Contains("all:admin"))));
            });
            #endregion

            #region Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Intelligence Hub API",
                    Version = "v1",
                    Description = "An API that simplifies utilizing and designing intelligent systems, particularly with AGI."
                });

                // Define the security scheme for bearer tokens
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by a space and the JWT token."
                });

                // Apply the security scheme globally
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

            #region Build App
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.WithOrigins("http://localhost:3000") // Specify allowed origin explicitly
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowed((host) => true);
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
            #endregion
        }
    }
}