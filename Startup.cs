using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SalesBotApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Collections.Generic;
using SalesBotApi.Controllers;
using Microsoft.Extensions.Options;

namespace SalesBotApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            IConfigurationSection mySettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<MySettings>(mySettingsSection);
            IConfigurationSection connectionStringsSection = Configuration.GetSection("ConnectionStrings");
            services.Configure<MyConnectionStrings>(connectionStringsSection);

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SalesChat.bot API", Version = "v1" });
            });
            services.AddSingleton<CosmosDbService>();
            services.AddSingleton<EmailService>();
            services.AddSingleton<SharedQueriesService>();
            services.AddSingleton<InMemoryCacheService<Company>>();
            services.AddSingleton<InMemoryCacheService<Conversation>>();
            services.AddSingleton<InMemoryCacheService<IEnumerable<Refinement>>>();
            services.AddSingleton<RedisCacheService<Company>>();
            services.AddSingleton<RedisCacheService<Conversation>>();
            services.AddSingleton<RedisCacheService<IEnumerable<Refinement>>>();
            
            services.AddSingleton<JwtService>();
            services.AddSingleton<PasswordHasherService>();
            services.AddSingleton<OpenAiHttpRequestService>();
            services.AddSingleton<ConfigController>();
            
            services.AddSingleton<LogBufferService>();
            services.AddSingleton<MetricsBufferService>();

            // Email Queue Service
            services.AddSingleton(serviceProvider =>
            {
                var myConnectionStrings = serviceProvider.GetRequiredService<IOptions<MyConnectionStrings>>();
                var logger = serviceProvider.GetRequiredService<LogBufferService>();
                var mySettings = serviceProvider.GetRequiredService<IOptions<MySettings>>().Value;
                string queueName = mySettings.QueueEmails;
                return new QueueService<EmailRequest>(queueName, myConnectionStrings, logger);
            });

            services.AddHostedService<LoggerBackgroundService>();
            services.AddHostedService<MetricsBackgroundService>();


            services.AddCors(options =>
                {
                    options.AddPolicy("AllowSpecificOrigin",
                        builder => builder.WithOrigins("*")
                                          .AllowAnyHeader()
                                          .AllowAnyMethod());
                });
            services.AddHttpClient();

            await OpenAiRequestBuilder.LoadOpenAiRequestContentJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SalesChat.bot API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseCors("AllowSpecificOrigin");

            app.UseRouting();

            app.UseAuthorization();

            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error is Exception ex)
                    {
                        logger.LogError(ex, "Unhandled exception occurred.");
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.\n");
                        await context.Response.WriteAsync(ex.ToString());
                    }
                });
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
