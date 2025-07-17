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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
using IntelligenceHub.Common.Interfaces;
using IntelligenceHub.Common.Implementations;
using IntelligenceHub.Host.Middleware;

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

            var stripeSettingsSection = builder.Configuration.GetSection(nameof(StripeSettings));

            builder.Services.Configure<Settings>(settingsSection);
            builder.Services.Configure<AuthSettings>(authSection);
            builder.Services.Configure<AppInsightSettings>(insightSettingsSection);
            builder.Services.Configure<StripeSettings>(stripeSettingsSection);

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
            builder.Services.AddScoped<IBillingService, StripeBillingService>();

            // Clients and Client Factory
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IUserIdAccessor, UserIdAccessor>();
            builder.Services.AddScoped<IUserCredentialProvider, UserCredentialProvider>();

            builder.Services.AddScoped<IAGIClientFactory, AGIClientFactory>();
            builder.Services.AddScoped<IRagClientFactory, RagClientFactory>();
            builder.Services.AddScoped<IAGIClient, AzureAIClient>();
            builder.Services.AddScoped<OpenAIClient>();
            builder.Services.AddScoped<AzureAIClient>();
            builder.Services.AddScoped<AnthropicAIClient>();
            builder.Services.AddScoped<IToolClient, ToolClient>();
            builder.Services.AddScoped<AzureAISearchServiceClient>();
            builder.Services.AddScoped<WeaviateSearchServiceClient>();
            builder.Services.AddScoped<IAIAuth0Client, Auth0Client>();

            // Repositories
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IToolRepository, ToolRepository>();
            builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
            builder.Services.AddScoped<IProfileToolsAssociativeRepository, ProfileToolsAssociativeRepository>();
            builder.Services.AddScoped<IMessageHistoryRepository, MessageHistoryRepository>();
            builder.Services.AddScoped<IIndexRepository, IndexRepository>();
            builder.Services.AddScoped<IIndexMetaRepository, IndexMetaRepository>();
            builder.Services.AddScoped<IUserServiceCredentialRepository, UserServiceCredentialRepository>();

            // Handlers
            builder.Services.AddSingleton(new LoadBalancingSelector(new Dictionary<string, string[]>()));
            builder.Services.AddSingleton<IValidationHandler, ValidationHandler>();
            builder.Services.AddSingleton<IBackgroundTaskQueueHandler, BackgroundTaskQueueHandler>();
            builder.Services.AddHostedService<BackgroundWorker>();

            #endregion

            #region Configure Client Policies

            // Define the ToolClient policy
            builder.Services.AddHttpClient(ClientPolicies.ToolClientPolicy.ToString()).AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(settings.ToolClientMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(settings.ToolClientInitialRetryDelay, retryAttempt))));

            // Define the Completion retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(settings.AGIClientMaxRetries, _ =>
                {
                    // Add random jitter up to 5 seconds.
                    var maxJitter = settings.AGIClientMaxJitter * 1000; // convert to milliseconds
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, maxJitter));
                    return TimeSpan.FromSeconds(settings.AGIClientInitialRetryDelay) + jitter;
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

            // Register policies for each service type (no-op without configuration)

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

            // Configure Auth
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

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddAuthentication("BasicAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
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

            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithOrigins(settings.ValidOrigins);
                });

                // Serve static files in development environment
                app.UseStaticFiles();
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    DefaultFileNames = new List<string> { "index.html" }
                });
            }
            else
            {
                app.UseCors(policy =>
                {
                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithOrigins(settings.ValidOrigins);
                });
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggingMiddleware>();

            app.UseAuthentication();
            app.UseMiddleware<UserContextMiddleware>();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream").RequireCors();

            app.Run();
            #endregion
        }
    }
}

