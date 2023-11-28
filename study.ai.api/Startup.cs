using Microsoft.AspNetCore.Builder;

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
            services.AddSwaggerGen();
            services.AddCors(options =>
            {
                options.AddPolicy("MyAllowSpecificOrigins",
                builder =>
                {
                    builder.WithOrigins("http://localhost:3000") // Replace with your client app's URL
                           .AllowAnyHeader()
                           .AllowAnyMethod(); // This allows all methods, including POST
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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
