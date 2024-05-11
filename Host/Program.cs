using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace OpenAICustomFunctionCallingAPI.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

//using OpenAICustomFunctionCallingAPI.Host.Config;
//using OpenAICustomFunctionCallingAPI.Hubs;
//using OpenAICustomFunctionCallingAPI.Business;
//using Microsoft.OpenApi.Models;
//using Microsoft.AspNet.SignalR;

//namespace OpenAICustomFunctionCallingAPI.Host
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            //var client = new SignalRMasterClient("http://localhost:7297/signalr");

//            var builder = WebApplication.CreateBuilder(args);

//            builder.Configuration.AddJsonFile("appsettings.json");
//            var settings = builder.Configuration.GetSection("Configurations").Get<Settings>();

//            builder.Services.AddSingleton(settings);
//            builder.Services.AddScoped<ICompletionLogic, CompletionLogic>();

//            builder.Services.AddSignalR();
//            builder.Services.AddControllers();
//            builder.Services.AddEndpointsApiExplorer();
//            builder.Services.AddSwaggerGen();

//            builder.Services.AddCors(options =>
//            {
//                options.AddPolicy("AllowAllOrigins",
//                    builder =>
//                    {
//                        builder.AllowAnyOrigin()
//                               .AllowAnyMethod()
//                               .AllowAnyHeader();
//                    });
//            });

//            builder.Services.AddSwaggerGen(options =>
//            {
//                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Some API v1", Version = "v1" });
//                options.AddSignalRSwaggerGen(ssgOptions => ssgOptions.ScanAssemblies(typeof(CompletionHub).Assembly));
//                //options.AddSignalRSwaggerGen();
//            });

//            var app = builder.Build();

//            if (app.Environment.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//                app.UseSwagger();
//                app.UseSwaggerUI();
//            }

//            app.UseHttpsRedirection();
//            app.UseRouting();

//            app.Run();
//        }
//    }
//}