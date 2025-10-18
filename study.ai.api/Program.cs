using Serilog;

namespace study.ai.api
{
    public class Program
    {
        private static IConfiguration _configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .Build();

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting quizcraftai API service.");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex) 
            {
                return 1;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureAppConfiguration(appConfiguration =>
                {

                });
                webBuilder.UseStartup<Startup>();
            });
    }
}