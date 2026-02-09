using AgenticApiDemo.Infrastructure.Data;
using AgenticApiDemo.Infrastructure.Plugins;
using AgenticApiDemo.Services;
using AgenticApiDemo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI; 
using Serilog;

namespace AgenticApiDemo.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Database
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("User=") || connectionString.Contains("Server=")))
                {
                    Log.Information("Using MySQL Database");
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                        mysqlOptions =>
                        {
                            mysqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorNumbersToAdd: null);
                        });
                }
                else
                {
                    Log.Information("Using SQLite Database");
                    if (string.IsNullOrEmpty(connectionString)) 
                         connectionString = "Data Source=agentic.db";
                    options.UseSqlite(connectionString);
                }
            });

            // Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFallbackAgentService, FallbackAgentService>();
            
            // Plugins (Now registered as Scoped because they depend on Scoped Services)
            services.AddScoped<UserApiPlugin>();

            // AI / Semantic Kernel
            services.AddScoped<Kernel>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var modelId = config["AI:ModelId"] ?? "llama3.2:3b";
                var endpoint = config["AI:Endpoint"] ?? "http://localhost:11434"; 

                var kernelBuilder = Kernel.CreateBuilder();

                // Using OpenAI Connector to talk to Ollama (Standard approach to avoid alpha package bugs)
                var ollamaEndpoint = $"{endpoint.TrimEnd('/')}/v1";
                
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: "ollama", // Dummy key for local Ollama
                    httpClient: new HttpClient { BaseAddress = new Uri(ollamaEndpoint) });

                var userApiPlugin = sp.GetRequiredService<UserApiPlugin>();
                kernelBuilder.Plugins.AddFromObject(userApiPlugin, "UserApi");

                kernelBuilder.Services.AddSingleton<ILoggerFactory>(sp.GetRequiredService<ILoggerFactory>());

                return kernelBuilder.Build();
            });

            return services;
        }
    }
}
