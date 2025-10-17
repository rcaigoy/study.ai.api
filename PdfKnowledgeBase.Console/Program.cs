using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PdfKnowledgeBase.Console.Services;
using PdfKnowledgeBase.Console.Helpers;
using PdfKnowledgeBase.Lib.Extensions;
using System;
using System.Threading.Tasks;
using study.ai.api.Models;
using Serilog;

namespace PdfKnowledgeBase.Console;

/// <summary>
/// Main entry point for the PDF Knowledge Base Console Application.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog for production-ready logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/pdf-kb-console-.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting PDF Knowledge Base Console Application");
            
            System.Console.WriteLine("=== PDF Knowledge Base Console Application ===");
            System.Console.WriteLine("Welcome to the interactive PDF knowledge base testing tool!");
            System.Console.WriteLine();

            // Build the host with dependency injection
            var host = CreateHostBuilder(args).Build();
            
            // Get the main service and run the application
            using var scope = host.Services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<KnowledgeBaseTester>();
            await app.RunAsync();

            Log.Information("Application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                var environmentName = context.HostingEnvironment.EnvironmentName;
                var basePath = AppContext.BaseDirectory;
                
                config.SetBasePath(basePath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
                
                // Add user secrets for development
                if (environmentName == "Development")
                {
                    config.AddUserSecrets<Program>();
                }
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
                
                var logLevel = context.Configuration.GetValue<string>("Application:LogLevel");
                if (Enum.TryParse<LogLevel>(logLevel, out var parsedLogLevel))
                {
                    logging.SetMinimumLevel(parsedLogLevel);
                }
                else
                {
                    logging.SetMinimumLevel(context.HostingEnvironment.IsProduction() ? LogLevel.Information : LogLevel.Debug);
                }
            })
            .ConfigureServices((context, services) =>
            {
                var chatGptApiKey = PrivateValues.ChatGPTApiKey;
                var isProduction = context.HostingEnvironment.IsProduction();
                
                // Add PDF Knowledge Base services with production settings
                services.AddPdfKnowledgeBase(options =>
                {
                    options.ChatGptApiKey = chatGptApiKey;
                    options.HttpTimeoutSeconds = context.Configuration.GetValue<int>("ChatGpt:TimeoutSeconds", isProduction ? 60 : 30);
                    options.DefaultSessionExpirationHours = context.Configuration.GetValue<int>("DocumentProcessing:DefaultExpirationHours", isProduction ? 4 : 2);
                    options.MaxFileSizeMB = context.Configuration.GetValue<int>("DocumentProcessing:MaxTemporaryFileSizeMB", isProduction ? 50 : 10);
                    options.DefaultChunkSize = context.Configuration.GetValue<int>("DocumentProcessing:DefaultChunkSize", 1500);
                    options.DefaultChunkOverlap = context.Configuration.GetValue<int>("DocumentProcessing:DefaultChunkOverlap", 300);
                });

                // Add console-specific services
                services.AddScoped<KnowledgeBaseTester>();
                services.AddScoped<PdfUploader>();
                services.AddScoped<QueryInterface>();
                services.AddScoped<SessionManager>();
                services.AddScoped<ConfigurationHelper>();
                services.AddScoped<ConsoleHelper>();
                
                // Add health check services for production monitoring
                services.AddHealthChecks();
            });
}
