﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NerdDinner.Web;
using NerdDinner.Web.Models;
using NerdDinner.Web.Persistence;
using System;
using Microsoft.AspNetCore.Identity;

namespace NerdDinner.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; private set; }
        public IHostingEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFrameworkSqlite().AddDbContext<NerdDinnerDbContext>(options =>
            {
                var connStringBuilder = new SqliteConnectionStringBuilder()
                {
                    DataSource = "./dinners.db"
                };
                options.UseSqlite(connStringBuilder.ToString());
            });

            services.AddTransient<INerdDinnerRepository, NerdDinnerRepository>();

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Home/AccessDenied";
            });

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<NerdDinnerDbContext>()
                    .AddDefaultTokenProviders();

            services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = "500918194801-v85iqffirr06ge97e5i901j1j455k9lp.apps.googleusercontent.com";
                options.ClientSecret = "5nvZDaPvNtoCqukUbuo2qEOF";
            })
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = "000000004012C08A";
                options.ClientSecret = "GaMQ2hCnqAC6EcDLnXsAeBVIJOLmeutL";
            })
            .AddTwitter(options =>
            {
                options.ConsumerKey = "lDSPIu480ocnXYZ9DumGCDw37";
                options.ConsumerSecret = "fpo0oWRNc3vsZKlZSq1PyOSoeXlJd7NnG4Rfc94xbFXsdcc3nH";
            });

            // Add MVC services to the services container
            services.AddMvc()
                .AddRazorPagesOptions(opts => {
                    opts.Conventions.AddPageRoute("/Index","dinners/{*pathInfo}");
                });

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
                options.AddPolicy("ManageDinner", authBuilder =>
                   {
                       authBuilder.RequireClaim("ManageDinner", "Allowed");
                   });
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
             if (HostingEnvironment.IsDevelopment())
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
            app.UseAuthentication();

            // Add MVC to the request pipeline
            app.UseMvc();
        }
    }
}