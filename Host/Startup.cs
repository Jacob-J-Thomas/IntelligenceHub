using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Hubs;
using OpenAICustomFunctionCallingAPI.Business;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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
            //services.AddEndpointsApiExplorer();

            //services.AddSwaggerGen(options =>
            //{
            //    //options.SwaggerDoc("v1", new OpenApiInfo { Title = "Some API v1", Version = "v1" });
            //});
            services.AddSwaggerGen();

            services.AddSingleton(_configuration.GetSection("Configurations").Get<Settings>());
            services.AddScoped<ICompletionLogic, CompletionLogic>();

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
            }

            app.UseFileServer();
            app.UseRouting();

            //app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chatstream");
                endpoints.MapControllers();
            });
        }
    }
}
