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
                    builder.WithOrigins("http://localhost:3000", "https://quizcraftai.com")
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
            
            // CORS must be placed AFTER UseRouting() and BEFORE UseEndpoints()
            // This is the correct order for ASP.NET Core to handle CORS properly
            app.UseCors("MyAllowSpecificOrigins");
            
            // Add exception handling AFTER CORS to ensure CORS headers are preserved
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    // CORS headers should already be added by the CORS middleware above
                    // But we ensure they're there by getting the policy and applying it
                    var corsService = context.RequestServices.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();
                    var corsPolicyProvider = context.RequestServices.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider>();
                    
                    if (corsService != null && corsPolicyProvider != null)
                    {
                        var policy = await corsPolicyProvider.GetPolicyAsync(context, "MyAllowSpecificOrigins");
                        if (policy != null)
                        {
                            var corsResult = corsService.EvaluatePolicy(context, policy);
                            corsService.ApplyResult(corsResult, context.Response);
                        }
                    }
                    
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"An error occurred processing your request.\"}");
                });
            });
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
