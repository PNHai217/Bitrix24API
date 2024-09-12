using Bitrix24API.Controllers;
using Bitrix24API.Data;
using Bitrix24API.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bitrix24API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            //var conStr = builder.Configuration.GetConnectionString("ConnectionStr");
            builder.Services.AddHttpClient();
            //builder.Services.AddSingleton<TokenService>();      

            /*   builder.Services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionStr")));
            */
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();
            // Configure the HTTP request pipeline.
            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=OAuth}/{action=Index}/{id?}");

            app.Run();
        }
    }
}