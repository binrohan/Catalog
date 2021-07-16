using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Catalog.Repositories;
using Catalog.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Catalog
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration for mongodb
            // Configure unknown data type of mongodb converting into something known to mongodb
            // While saveing data in mongodb
            // Converting Guid => string
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            // Converting DateTimeOffsetSerializer => string
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
            // MongoDbSettings from appsettings.json file and Map it to the MongoDbSettings class
            var mongoDbSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

            // Configuration for mongodb
            // Adding mongodb connection string
            services.AddSingleton<IMongoClient>(servicesProvider => 
            {
                // Connecting mongodb to the backend
                return new MongoClient(mongoDbSettings.ConnectionString);
            });

            // Registering the service
            // Add service interface for two classes
            services.AddSingleton<IItemsRepository, MongoDbItemsRepository>();

            services.AddControllers(options => {
                // Following option for telling .Net 5 not to remove Async suffix from method Name
                options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog", Version = "v1" });
            });

            // Connection check of REST API
            services.AddHealthChecks()
                // Mongodb connection healthcheck configuration
                .AddMongoDb(
                    mongoDbSettings.ConnectionString, 
                    name: "mongodb", 
                    timeout: TimeSpan.FromSeconds(3),
                    tags: new [] {"ready"});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // Setup health check route
                // Is Server ready check config
                endpoints.MapHealthChecks("/heath/ready", new HealthCheckOptions{
                    Predicate = (check) => check.Tags.Contains("ready"),
                    // Customizing the heath check response
                    ResponseWriter = async(context, report) => 
                    {
                        var result = JsonSerializer.Serialize
                        (
                            new
                            {
                                status = report.Status.ToString(),
                                checks = report.Entries.Select(entry => new 
                                {
                                    name = entry.Key,
                                    status = entry.Value.Status.ToString(),
                                    exception = entry.Value.Exception != null 
                                        ? entry.Value.Exception.Message
                                        : "none",
                                    duration = entry.Value.Duration.ToString()
                                })
                            }
                        );
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(result);
                    }
                });
                // Is server alive check config
                endpoints.MapHealthChecks("/heath/live", new HealthCheckOptions{
                    Predicate = (_) => false
                });
            });
        }
    }
}
