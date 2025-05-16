using ItemDescTableModder.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ItemDescTableModder
{
    public interface IConfigLoader
    {
        Config Load();
    }


    public class ConfigLoader(ILogger<ConfigLoader> logger, string executableDirectory) : IConfigLoader
    {
        private readonly ILogger<ConfigLoader> _logger = logger;
        private readonly string _workingDirectory = executableDirectory;

        public Config Load()
        {
            var configFileName = $"{typeof(Program).Namespace}.conf";
            string configPath = Path.Combine(_workingDirectory, configFileName);

            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Config {configFileName} not found. Creating default config file.", configFileName);
                var defaultConfig = GenerateDefaultConfig();
                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(configPath, json);
                return defaultConfig;
            }

            try
            {
                _logger.LogInformation("Loading config {configFileName}", configFileName);
                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<Config>(json);
                return config ?? GenerateDefaultConfig();
            }
            catch (Exception)
            {
                _logger.LogWarning("Invalid config file detected. Tool is going to use default configurations.");
                return GenerateDefaultConfig();
            }
        }

        private Config GenerateDefaultConfig()
        {
            return new Config
            {
                ItemIdDescTextColor = "007ACC",
                ItemIdDescValueColor = "FFB300",
                BrewingConfig = new TaggingConfig
                {
                    EnableTags = 1,
                    EnableDescriptions = 1,
                    TagText = "Brew",
                    DescriptionHeaderColor = "00897B",
                    DescriptionRowsColor = "43A047"
                },
                CookingConfig = new TaggingConfig
                {
                    EnableTags = 1,
                    EnableDescriptions = 1,
                    TagText = "Cook",
                    DescriptionHeaderColor = "EF6C00",
                    DescriptionRowsColor = "6D4C41"
                },
                QuestConfig = new TaggingConfig
                {
                    EnableTags = 1,
                    EnableDescriptions = 1,
                    TagText = "Quest",
                    DescriptionHeaderColor = "5E35B1",
                    DescriptionRowsColor = "8E24AA"
                },
                InstanceConfig = new TaggingConfig
                {
                    EnableTags = 1,
                    EnableDescriptions = 1,
                    TagText = "Instance",
                    DescriptionHeaderColor = "C62828",
                    DescriptionRowsColor = "D84315"
                }
            };
        }
    }
}
