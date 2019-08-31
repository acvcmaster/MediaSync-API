using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediaSync.Services;
using Swashbuckle.AspNetCore.Swagger;
using Newtonsoft.Json.Converters;

namespace MediaSync
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
            services.AddCors((options) => {
                options.AddPolicy("_all", (builder) => {
                    builder.WithOrigins("*");
                });
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2).AddJsonOptions(options =>
                options.SerializerSettings.Converters.Add(new StringEnumConverter()));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "MediaSync API", Version = "v1" });
            }).ConfigureSwaggerGen((options) => {
                options.OperationFilter<FileUploadOperation>(); //Register File Upload Operation Filter
            });
#if DEBUG
            services.AddSingleton<IFileService, FileService>((service) => new FileService(Configuration.GetSection("DefaultPathDev").Value));
#elif DOCKER
            services.AddSingleton<IFileService, FileService>((service) => new FileService(Configuration.GetSection("DefaultPathDocker").Value));
#else
            services.AddSingleton<IFileService, FileService>((service) => new FileService(Configuration.GetSection("DefaultPath").Value));
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediaSync API");
            });

            app.UseCors("_all");
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
