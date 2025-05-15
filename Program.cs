using ItemDescTableModder.Models;
using ItemDescTableModder.Services;
using ItemDescTableModder.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ItemDescTableModder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string executableDirectory = Environment.CurrentDirectory;
            string configPath = Path.Combine(executableDirectory, $"{typeof(Program).Namespace}.conf");

            // Load or create config
            Config config = LoadOrCreateConfig(configPath);

            // Setup DI
            var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole())
                .AddSingleton(Options.Create(config))
                .AddSingleton<IApp, App>()
                .AddSingleton(executableDirectory)
                .AddScoped<ILuaTableHandler, LuaTableHandler>()
                .AddScoped<ILuaTableModifier, LuaTableModifier>()
                .AddScoped<ILuaTableSerializer, LuaTableSerializer>()
                .BuildServiceProvider();

            // Get service
            var app = serviceProvider.GetRequiredService<IApp>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Check if a file was provided as an argument (dragged onto the exe)
                if (args.Length > 0 && File.Exists(args[0]))
                {

                    string filePath = args[0];
                    logger.LogInformation("Processing file: {filePath}", Path.GetFileName(filePath));
                    app.ProcessFile(filePath);
                    logger.LogInformation("Processing completed successfully.");
                }
                else
                {
                    logger.LogWarning("No file provided or file does not exist.");
                    Console.WriteLine("Please drag a file onto the application to process it.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the file.");
            }

            // Keep console window open
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static Config LoadOrCreateConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                // Create a default config
                var defaultConfig = new Config();
                string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                return defaultConfig;
            }

            try
            {
                // Load existing config
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Config>(json);
                return config ?? new Config();
            }
            catch (Exception)
            {
                // If there's an error, return default config
                return new Config();
            }
        }

    }
}
