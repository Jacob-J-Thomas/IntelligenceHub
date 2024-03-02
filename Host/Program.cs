using OpenAICustomFunctionCallingAPI.Host.Config;
using Polly.Contrib.WaitAndRetry;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using OpenAICustomFunctionCallingAPI.DAL;// refactor this dependency when you have time
using System.Reflection;

namespace OpenAICustomFunctionCallingAPI.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json");
            var settings = builder.Configuration.GetSection("Configurations").Get<Settings>();

            // Add controllers
            builder.Services.AddSingleton(settings);
            builder.Services.AddControllers();

            // Add databases
            //builder.Services.AddDbContext<ProfileDb>(options => options.UseSqlServer(settings.DbConnectionString));
            //builder.Services.AddScoped(typeof(IRepository<>), typeof(ProfileRepository<>));

            //var modelBuilder = new ModelBuilder();
            //modelBuilder.Entity<Type>().Property(u => u.GetProperty(u.ToString())).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}