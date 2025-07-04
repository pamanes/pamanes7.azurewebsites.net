using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
            {
                ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
            });
            //dotnet ef migrations add InitBlogSchema
            //dotnet ef database update
            builder.Services.AddDbContext<MDBlogDbContext>(options => options.UseSqlite($"Data Source={builder.Configuration["DBPath"]}")
                //.LogTo(Console.WriteLine, LogLevel.Information)
                //.EnableSensitiveDataLogging()
            );
            builder.Services.AddSingleton(provider =>
            {
                return new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseBootstrap()
                    .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // adds ids to headings
                    .Build();
            });
            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Admin/Login";
                });

            builder.Services.Configure<AdminAuthSettings>(builder.Configuration.GetSection("AdminAuth"));
            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();


            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapDefaultControllerRoute();

            app.MapControllerRoute(
                name: "PostByDateSlug",
                pattern: "{year:int}/{month:int}/{day:int}/{slug}.html",
                defaults: new { controller = "Home", action = "PostByDateSlugMarkdig" });

            app.Run();
        }
    }
}
