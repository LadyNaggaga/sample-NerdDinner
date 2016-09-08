using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NerdDinner.Web.Models;
using NerdDinner.Web.Persistence;
using System;

namespace NerdDinner.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
               .AddJsonFile($"secrets.json", optional: true)
               .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                //Add user secrets for more information please check out 
                builder.AddUserSecrets();
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            Configuration = builder.Build();

            HostingEnvironment = env;

        }

        public IConfiguration Configuration { get; private set; }

        public IHostingEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddEntityFrameworkSqlite().AddDbContext<NerdDinnerDbContext>(options => {
                var connStringBuilder = new SqliteConnectionStringBuilder()
                {
                    DataSource = "./dinners.db"
                };
                options.UseSqlite(connStringBuilder.ToString());
            });

            services.AddTransient<INerdDinnerRepository, NerdDinnerRepository>();

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Cookies.ApplicationCookie.AccessDeniedPath = "/Home/AccessDenied";

            })
                    .AddEntityFrameworkStores<NerdDinnerDbContext>()
                    .AddDefaultTokenProviders();


            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins("http://example.com");
                });
            });

            services.AddLogging();

            // Add MVC services to the services container
            services.AddMvc();

            // Add memory cache services
            if (HostingEnvironment.IsProduction())
            {
                services.AddMemoryCache();
                services.AddDistributedMemoryCache();
            }

            // Add session related services.
            // TODO: Test Session timeout
            services.AddSession(options =>
            {
                // options.CookieName = ".AdventureWorks.Session";
                options.IdleTimeout = TimeSpan.FromSeconds(10);
            });

            // Add the system clock service
            services.AddSingleton<ISystemClock, SystemClock>();

            // Configure Auth
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "ManageDinner",
                    authBuilder =>
                    {
                        authBuilder.RequireClaim("ManageDinner", "Allowed");
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            // Configure Session.
            app.UseSession();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
            app.UseIdentity();
            //The following lines starting with app.UseThirdPartyAuthenicaiton enable logging in 
            //with login providers https://docs.asp.net/en/latest/security/authentication/sociallogins.html
            app.UseFacebookAuthentication(new FacebookOptions
            {
                //TODO: HideKeys all authentications
                AppId = "Authentication:Facebook:AppId",
                AppSecret = "Authentication:Facebook:AppSecret"
            });

            app.UseGoogleAuthentication(new GoogleOptions
            {
                ClientId = "Authentication:Google:ClientId",
                ClientSecret = "Authentication:Google:ClientSecret"
            });

            app.UseTwitterAuthentication(new TwitterOptions
            {
                ConsumerKey = "Authentication:Twitter:ConsumerKey",
                ConsumerSecret = "Authentication:Twitter:ConsumerSecret"
            });

            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountOptions
            {
                DisplayName = "MicrosoftAccount - Requires project changes",
                ClientId = "Authentication:Microsoft:ClientId",
                ClientSecret = "Authentication:Microsoft:ClientSecret"
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "redirectjsdinner",
                    template: "dinners/{*pathInfo}",
                    defaults: new { controller = "Home", action = "Index" }
                    );
                routes.MapRoute(
                     name: "default",
                     template: "{controller}/{action}/{id?}",
                     defaults: new { controller = "Home", action = "Index" });

            });

            //Populate dinner sample data
            SampleData.InitializeNerdDinner(app.ApplicationServices).Wait();
        }
    }
}