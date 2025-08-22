using DotNetEnv;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Host.Logging;
using IntelligenceHub.Host.Policies;
using IntelligenceHub.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Host.Swagger;
using IntelligenceHub.Business.Handlers;
using Microsoft.AspNetCore.Authentication;
using IntelligenceHub.DAL.Tenant;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.Host
{
    /// <summary>
    /// Entry point for the IntelligenceHub web host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Bootstraps and runs the web application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Add Services and Settings

            var settingsSection = builder.Configuration.GetRequiredSection(nameof(Settings));
            var settings = settingsSection.Get<Settings>();

            var authSection = builder.Configuration.GetRequiredSection(nameof(AuthSettings));
            var authSettings = authSection.Get<AuthSettings>();

            var insightSettingsSection = builder.Configuration.GetRequiredSection(nameof(AppInsightSettings));
            var insightSettings = insightSettingsSection.Get<AppInsightSettings>();

            var agiClientSettingsSection = builder.Configuration.GetRequiredSection(nameof(AGIClientSettings));
            var agiClientSettings = agiClientSettingsSection.Get<AGIClientSettings>();
            var featureFlagsSection = builder.Configuration.GetSection(nameof(FeatureFlagSettings));

            builder.Services.Configure<Settings>(settingsSection);
            builder.Services.Configure<AuthSettings>(authSection);
            builder.Services.Configure<AppInsightSettings>(insightSettingsSection);
            builder.Services.Configure<AGIClientSettings>(agiClientSettingsSection);
            builder.Services.Configure<FeatureFlagSettings>(featureFlagsSection);
            builder.Services.Configure<AzureSearchServiceClientSettings>(builder.Configuration.GetRequiredSection(nameof(AzureSearchServiceClientSettings)));
            builder.Services.Configure<WeaviateSearchServiceClientSettings>(builder.Configuration.GetSection(nameof(WeaviateSearchServiceClientSettings)));

            // Register AuthSettings as a singleton
            builder.Services.AddSingleton(authSettings);

            // Add Services

            // Add EF Core DbContext with Basic Retry Policy
            builder.Services.AddDbContext<IntelligenceHubDbContext>(options =>
                options.UseSqlServer(settings.DbConnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxDbRetries,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.MaxDbRetryDelay),
                        errorNumbersToAdd: null // Provide specific codes here if desired
                    );
                })
            );

            // Logic
            builder.Services.AddScoped<ICompletionLogic, CompletionLogic>();
            builder.Services.AddScoped<IMessageHistoryLogic, MessageHistoryLogic>();
            builder.Services.AddScoped<IProfileLogic, ProfileLogic>();
            builder.Services.AddScoped<IRagLogic, RagLogic>();
            builder.Services.AddScoped<IAuthLogic, AuthLogic>();
            builder.Services.AddScoped<IUserLogic, UserLogic>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
            builder.Services.AddScoped<IUsageService, UsageService>();

            // Clients and Client Factory
            builder.Services.AddSingleton<IAGIClientFactory, AGIClientFactory>();
            builder.Services.AddSingleton<IRagClientFactory, RagClientFactory>();
            builder.Services.AddSingleton<AzureAIClient>(sp =>
                new AzureAIClient(
                    sp.GetRequiredService<IOptionsMonitor<AGIClientSettings>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    AGIServiceHost.Azure));
            builder.Services.AddSingleton<OpenAIClient>(sp =>
                new OpenAIClient(
                    sp.GetRequiredService<IOptionsMonitor<AGIClientSettings>>(),
                    sp.GetRequiredService<IHttpClientFactory>()));
            builder.Services.AddSingleton<AnthropicAIClient>(sp =>
                new AnthropicAIClient(
                    sp.GetRequiredService<IOptionsMonitor<AGIClientSettings>>(),
                    sp.GetRequiredService<IHttpClientFactory>()));
            builder.Services.AddSingleton<IAGIClient>(sp => sp.GetRequiredService<AzureAIClient>());
            builder.Services.AddSingleton<IToolClient, ToolClient>();
            builder.Services.AddSingleton<AzureAISearchServiceClient>();
            builder.Services.AddSingleton<WeaviateSearchServiceClient>();
            builder.Services.AddSingleton<IAIAuth0Client, Auth0Client>();
            builder.Services.AddScoped<ITenantProvider, TenantProvider>();

            // Repositories
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IToolRepository, ToolRepository>();
            builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
            builder.Services.AddScoped<IProfileToolsAssociativeRepository, ProfileToolsAssociativeRepository>();
            builder.Services.AddScoped<IMessageHistoryRepository, MessageHistoryRepository>();
            builder.Services.AddScoped<IIndexRepository, IndexRepository>();
            builder.Services.AddScoped<IIndexMetaRepository, IndexMetaRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Handlers
            var serviceUrls = new Dictionary<string, string[]>();
            if (agiClientSettings?.AzureOpenAIServices != null) serviceUrls.Add(ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureOpenAIServices.Select(service => service.Endpoint).ToArray());
            if (agiClientSettings?.OpenAIServices != null) serviceUrls.Add(ClientPolicies.OpenAIClientPolicy.ToString(), agiClientSettings.OpenAIServices.Select(service => service.Endpoint).ToArray());
            if (agiClientSettings?.AnthropicServices != null) serviceUrls.Add(ClientPolicies.AnthropicAIClientPolicy.ToString(), agiClientSettings.AnthropicServices.Select(service => service.Endpoint).ToArray());

            builder.Services.AddSingleton(new LoadBalancingSelector(serviceUrls));
            builder.Services.AddSingleton<IValidationHandler, ValidationHandler>();
            builder.Services.AddSingleton<IBackgroundTaskQueueHandler, BackgroundTaskQueueHandler>();
            builder.Services.AddHostedService<BackgroundWorker>();
            builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

            #endregion

            #region Configure Client Policies

            // Define the ToolClient policy
            builder.Services.AddHttpClient(ClientPolicies.ToolClientPolicy.ToString()).AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(settings.ToolClientMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(settings.ToolClientInitialRetryDelay, retryAttempt))));

            // Define the Completion retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError() // 5xx, 408, HttpRequestException
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                               || (int)r.StatusCode == 529) // Anthropic "overloaded"
                .WaitAndRetryAsync(
                    settings.AGIClientMaxRetries,
                    _ =>
                    {
                        // jittered backoff
                        var jitterMs = new Random().Next(0, settings.AGIClientMaxJitter * 1000);
                        return TimeSpan.FromSeconds(settings.AGIClientInitialRetryDelay) + TimeSpan.FromMilliseconds(jitterMs);
                    });

            // Define the Completion circuit breaker policy if more than one service exists.
            IAsyncPolicy<HttpResponseMessage> policyWrap = retryPolicy;

            // Register the HttpClient with the load balancing handler and appropriate policy for each AI client type. -> Services with more than 1 instance will have a circuit breaker policy.
            void RegisterClientPolicy(IServiceCollection services, string policyName, string serviceName, int serviceCount)
            {
                IAsyncPolicy<HttpResponseMessage> policy = retryPolicy;

                if (serviceCount > 1)
                {
                    var circuitBreakerPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: settings.MaxCircuitBreakerFailures,
                        durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerBreakDuration),
                        onBreak: (result, breakDelay) =>
                        {
                            Console.WriteLine($"Circuit opened for {breakDelay.TotalSeconds} seconds.");
                        },
                        onReset: () => Console.WriteLine("Circuit closed - normal operation resumed."),
                        onHalfOpen: () => Console.WriteLine("Circuit is half-open - trial requests allowed.")
                    );

                    // Combine the Completion retry and circuit breaker policies.
                    policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
                }

                LoadBalancingSelector.RegisterHttpClientWithPolicy(services, policyName, serviceName, policy);
            }

            // Register policies for each service type
            RegisterClientPolicy(builder.Services, ClientPolicies.AzureAIClientPolicy.ToString(), ClientPolicies.AzureAIClientPolicy.ToString(), agiClientSettings.AzureOpenAIServices.Count);
            RegisterClientPolicy(builder.Services, ClientPolicies.OpenAIClientPolicy.ToString(), ClientPolicies.OpenAIClientPolicy.ToString(), agiClientSettings.OpenAIServices.Count);
            RegisterClientPolicy(builder.Services, ClientPolicies.AnthropicAIClientPolicy.ToString(), ClientPolicies.AnthropicAIClientPolicy.ToString(), agiClientSettings.AnthropicServices.Count);

            #endregion

            #region Json Serialization Settings

            // Configure json serialization for controllers and hubs
            builder.Services.AddSignalR().AddJsonProtocol(options =>
            {
                // Set serialization for global enums utilized in DTOs
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }).AddHubOptions<ChatHub>(options =>
            {
                if (builder.Environment.IsDevelopment()) options.EnableDetailedErrors = true; // enable detailed errors for dev environments
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

            // Configure API key authentication
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ApiKey";
                options.DefaultChallengeScheme = "ApiKey";
            });

            authBuilder.AddScheme<AuthenticationSchemeOptions, Host.Auth.ApiKeyAuthenticationHandler>("ApiKey", null);

            if (builder.Environment.IsDevelopment())
            {
                authBuilder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
            }

            // Add role-based authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(ElevatedAuthPolicy, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => (c.Type == "scope" || c.Type == "permissions") && c.Value.Split(' ').Contains("all:admin"))));
            });
            #endregion

            #region Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                // Add Swagger documentation
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Intelligence Hub API",
                    Version = "v1",
                    Description = "An API that simplifies utilizing and designing intelligent systems, particularly with AGI."
                });

                // Used to mark nullable path parameters as such
                options.OperationFilter<NullableRouteParametersOperationFilter>();

                // Enable annotations to set NSwag generated names for client methods
                options.EnableAnnotations();

                // Define the security scheme for API key authentication
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "X-Api-Key",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "API key used for authentication"
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
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            #endregion

            #region Build App

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DefaultCors", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                // Serve static files in development environment
                app.UseStaticFiles();
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    DefaultFileNames = new List<string> { "index.html" }
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("DefaultCors");

            app.UseMiddleware<LoggingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream").RequireCors("DefaultCors");

            app.Run();
            #endregion
        }
    }
}

