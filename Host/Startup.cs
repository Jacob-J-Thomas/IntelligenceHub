using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Hubs;
using OpenAICustomFunctionCallingAPI.Business;

namespace OpenAICustomFunctionCallingAPI.Host
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

            services.AddSingleton(_configuration.GetSection("Configurations").Get<Settings>());
            services.AddScoped<ICompletionLogic, CompletionLogic>();

            //string[] allowedOrigins = new string[]
            //{
            //    "http://localhost:8080",
            //    "https://localhost:8080",
            //    "https://localhost:8080/index.html",
            //    "https://localhost:53337/chatstream",
            //    "https://localhost:53337/chatstream/negotiate",
            //    "http://localhost:8080/index.html",
            //    "http://localhost:53337/chatstream",
            //    "http://localhost:53337/chatstream/negotiate"
            //};
            services.AddSignalR();
            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .WithOrigins("https://localhost:8080") // Specify allowed origins
                        .AllowAnyMethod() // Allow any HTTP method (GET, POST, etc.)
                        .AllowAnyHeader() // Allow any headers
                        .AllowCredentials()); // Allow credentials (cookies, etc.)
            });


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            

            app.UseFileServer();
            app.UseRouting();

            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chatstream");
                endpoints.MapControllers();
            });
        }
    }
}
