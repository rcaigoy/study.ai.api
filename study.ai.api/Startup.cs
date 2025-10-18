using Serilog;
using Microsoft.AspNetCore.Builder;
using PdfKnowledgeBase.Lib.Extensions;
using study.ai.api.Models;

namespace study.ai.api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMvc();
            services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen();
            services.AddCors(options =>
            {
                options.AddPolicy("MyAllowSpecificOrigins",
                builder =>
                {
                    builder.WithOrigins("http://localhost:3000","https://quizcraftai.com") // Replace with your client app's URL
                           .AllowAnyHeader()
                           .AllowAnyMethod(); // This allows all methods, including POST
                });
            });

            // Register PDF Knowledge Base services
            services.AddPdfKnowledgeBase(options =>
            {
                options.ChatGptApiKey = PrivateValues.ChatGPTApiKey;
                options.HttpTimeoutSeconds = 60;
                options.DefaultSessionExpirationHours = 2;
                options.MaxFileSizeMB = 50;
                options.DefaultChunkSize = 1500;
                options.DefaultChunkOverlap = 300;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();


            app.UseRouting();
            app.UseCors("MyAllowSpecificOrigins");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
