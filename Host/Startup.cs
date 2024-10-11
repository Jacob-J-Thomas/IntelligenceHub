using IntelligenceHub.Hubs;
using IntelligenceHub.Business;
using Polly;
using Polly.Extensions.Http;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Host
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen();

            services.AddSingleton(_configuration.GetSection("Settings").Get<Settings>());
            services.AddSingleton(_configuration.GetSection("AIClientSettings").Get<AIClientSettings>());
            services.AddSingleton(_configuration.GetSection("SearchServiceSettings").Get<SearchServiceClientSettings>());
            services.AddScoped<ICompletionLogic, CompletionLogic>();

            // Configure HttpClient with Polly retry policy
            services.AddHttpClient("FunctionClient")
                    .AddPolicyHandler(GetRetryPolicy());

            services.AddSignalR();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
