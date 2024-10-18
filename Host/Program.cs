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
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using IntelligenceHub.Controllers.Auth;
using Microsoft.AspNetCore.Authorization;

namespace IntelligenceHub.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSwaggerGen();

            // Add Services
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(Settings)).Get<Settings>());
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(AGIClientSettings)).Get<AGIClientSettings>());
            builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(nameof(SearchServiceClientSettings)).Get<SearchServiceClientSettings>());
            builder.Services.AddSingleton<IAGIClient, AGIClient>();
            builder.Services.AddSingleton<IAISearchServiceClient, AISearchServiceClient>();
            builder.Services.AddSingleton<ICompletionLogic, CompletionLogic>();
            builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            // Configure HttpClient with Polly retry policy
            builder.Services.AddHttpClient("FunctionClient").AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

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

            // Configure Auth
            var authSettings = builder.Configuration.GetRequiredSection(nameof(AuthSettings)).Get<AuthSettings>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = authSettings.Domain;
                options.Audience = authSettings.Audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("read:messages", policy => policy.Requirements.Add(
                    new HasScopeRequirement("read:messages", authSettings.Domain)));
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseFileServer();
            app.UseRouting();

            app.MapControllers();
            app.MapHub<ChatHub>("/chatstream");

            app.Run();
        }
    }
}