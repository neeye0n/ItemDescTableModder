using ItemDescTableModder.Models;
using ItemDescTableModder.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoonSharp.Interpreter;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ItemDescTableModder
{
    public interface IApp
    {
        Task ProcessFile(string filePath);
    }

    public class App : IApp
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<App> _logger;
        private readonly ILuaTableHandler _tableHandler;
        private readonly ILuaTableModifier _tableModifier;
        private readonly Config _config;
        private readonly string _workingDir;
        private readonly string _outputDirectory;
        private readonly string _outputFilename;
        private readonly string _instanceMatsTable;
        private readonly string _brewMatsTable;
        private readonly string _questMatsTable;
        private readonly string _cookinMatsTable;

        public App(IHttpClientFactory factory, ILogger<App> logger, ILuaTableHandler tableHandler, ILuaTableModifier tableModifier, string executableDirectory, IOptions<Config> config)
        {
            _httpClient = factory.CreateClient("ItemDescTableModder");
            _logger = logger;
            _tableHandler = tableHandler;
            _tableModifier = tableModifier;
            _outputFilename = "itemInfo_EN.lua";
            _outputDirectory = "System";

            _instanceMatsTable = "instanceMatsTable.json";
            _brewMatsTable = "brewingMatsTable.json";
            _questMatsTable = "questMatsTable.json";
            _cookinMatsTable = "cookingMatsTable.json";

            _workingDir = executableDirectory;
            _config = config.Value;
        }

        public async Task ProcessFile(string filePath)
        {
            _logger.LogInformation("Starting to process the file...");

            try
            {
                var table = _tableHandler.LoadFile(filePath, "tbl");
                var allItemIds = _tableModifier.GetItemIds(table);
                var instanceTags = await GenerateMaterialTags(_instanceMatsTable);
                var brewingTags = await GenerateMaterialTags(_brewMatsTable);
                var questTags = await GenerateMaterialTags(_questMatsTable);
                var cookingTags = await GenerateMaterialTags(_cookinMatsTable);

                // Order of Precedence:
                // ItemId
                // Brewing
                // Cooking
                // Quest
                // Instance

                // Apply Instance
                _logger.LogInformation("Applying Instance descriptions and tags...");
                foreach (var kvp in instanceTags)
                {
                    _tableModifier.ModifyItem(table, kvp.Key, item =>
                    {
                        var instanceInfos = kvp.Value.Split("||");
                        if (instanceInfos.Length == 0)
                            return;

                        if (_config.InstanceConfig.EnableTags != 0)
                        {
                            var instanceTags = string.Join(", ", instanceInfos.Select(item =>
                            {
                                var parts = item.Split("&&&", StringSplitOptions.TrimEntries);
                                return $"{parts[0]} - {parts[1]}";
                            }));

                            var displayName = item.Get("identifiedDisplayName").String;
                            item.Set("identifiedDisplayName", DynValue.NewString(displayName.Trim() + " (" + instanceTags + ")"));
                        }

                        if (_config.InstanceConfig.EnableDescriptions != 0)
                        {
                            var descriptionTable = item.Get("identifiedDescriptionName").Table;

                            List<DynValue> newDescList = [DynValue.NewString($"^{_config.InstanceConfig.DescriptionHeaderColor}[Instance Material]^000000")];
                            foreach (var instanceInfo in instanceInfos)
                            {
                                var info = instanceInfo.Split("&&&", StringSplitOptions.TrimEntries);
                                newDescList.Add(DynValue.NewString($"^{_config.InstanceConfig.DescriptionRowsColor}{info[0].Trim()} - Qty: {info[1].Trim()}^000000"));
                            }
                            AddDescriptionsToTop(ref descriptionTable, newDescList);
                        }
                    });
                }

                // Apply Quest
                _logger.LogInformation("Applying Quest descriptions and tags...");
                foreach (var kvp in questTags)
                {
                    _tableModifier.ModifyItem(table, kvp.Key, item =>
                    {
                        var questInfos = kvp.Value.Split("||");
                        if (questInfos.Length == 0)
                            return;

                        if (_config.QuestConfig.EnableTags != 0)
                        {
                            var displayName = item.Get("identifiedDisplayName").String;
                            var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                            item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.QuestConfig.TagText}]" + startingSpace + displayName.Trim()));
                        }

                        if (_config.QuestConfig.EnableDescriptions != 0)
                        {
                            var descriptionTable = item.Get("identifiedDescriptionName").Table;

                            List<DynValue> newDescList = [DynValue.NewString($"^{_config.QuestConfig.DescriptionHeaderColor}[Quest Material]^000000")];
                            foreach (var questInfo in questInfos)
                            {
                                var info = questInfo.Split("&&&", StringSplitOptions.TrimEntries);
                                newDescList.Add(DynValue.NewString($"^{_config.QuestConfig.DescriptionRowsColor}{info[0].Trim()} - Qty: {info[1].Trim()}^000000"));
                            }
                            AddDescriptionsToTop(ref descriptionTable, newDescList);
                        }
                    });
                }

                // Apply Cooking
                _logger.LogInformation("Applying Cooking descriptions and tags...");
                foreach (var kvp in cookingTags)
                {
                    _tableModifier.ModifyItem(table, kvp.Key, item =>
                    {
                        var cookingInfos = kvp.Value.Split("||");
                        if (cookingInfos.Length == 0)
                            return;

                        if (_config.CookingConfig.EnableTags != 0)
                        {
                            var displayName = item.Get("identifiedDisplayName").String;
                            var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                            item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.CookingConfig.TagText}]" + startingSpace + displayName.Trim()));
                        }

                        if (_config.CookingConfig.EnableDescriptions != 0)
                        {
                            var descriptionTable = item.Get("identifiedDescriptionName").Table;

                            List<DynValue> newDescList = [DynValue.NewString($"^{_config.CookingConfig.DescriptionHeaderColor}[Cooking Material]^000000")];
                            foreach (var cookingInfo in cookingInfos)
                            {
                                var info = cookingInfo.Split("&&&", StringSplitOptions.TrimEntries);
                                newDescList.Add(DynValue.NewString($"^{_config.CookingConfig.DescriptionRowsColor}{info[0].Trim()} - Qty: {info[1].Trim()}^000000"));
                            }
                            AddDescriptionsToTop(ref descriptionTable, newDescList);
                        }
                    });
                }

                // Apply Brewing
                _logger.LogInformation("Applying Brewing descriptions and tags...");
                foreach (var kvp in brewingTags)
                {
                    _tableModifier.ModifyItem(table, kvp.Key, item =>
                    {
                        //var brewingInfos = kvp.Value.Split("||");
                        //if (brewingInfos.Length == 0)
                        //    return;
                        if (_config.BrewingConfig.EnableTags != 0)
                        {
                            var displayName = item.Get("identifiedDisplayName").String;
                            var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                            item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.BrewingConfig.TagText}]" + startingSpace + displayName.Trim()));
                        }

                        if (_config.BrewingConfig.EnableDescriptions != 0)
                        {
                            var descriptionTable = item.Get("identifiedDescriptionName").Table;
                            AddDescriptionsToTop(ref descriptionTable, [
                                DynValue.NewString($"^{_config.BrewingConfig.DescriptionHeaderColor}[Brewing Material]^000000")
                            ]);
                        }
                    });
                }

                // Apply Item Ids
                _logger.LogInformation("Applying Item IDs descriptions...");
                foreach (var itemId in allItemIds)
                {
                    _tableModifier.ModifyItem(table, itemId, item =>
                     {
                         var newDescList = new List<DynValue>
                         {
                        DynValue.NewString($"^{_config.ItemIdDescTextColor}Item ID:^{_config.ItemIdDescValueColor} {itemId}^000000")
                         };

                         var descriptionTable = item.Get("identifiedDescriptionName").Table;
                         AddDescriptionsToTop(ref descriptionTable, newDescList);
                     });
                }

                _tableHandler.SaveToFile(table, GetOutputFullPath());
                _logger.LogInformation("Done! Check the generated file in created System folder");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An uxexpected error has occurred. The application will terminate.");
            }
        }

        private async Task<Dictionary<int, string>> GenerateMaterialTags(string resourceName)
        {
            try
            {
                string json = await _httpClient.GetStringAsync(resourceName);
                var materialTable = JsonSerializer.Deserialize<Dictionary<string, List<MaterialInfo>>>(json);

                var materialTags = new Dictionary<int, string>();
                if (materialTable is not null)
                {
                    materialTags = materialTable
                        .SelectMany(kvp => kvp.Value.Select(item => new
                        {
                            Group = kvp.Key,
                            Item = item
                        }))
                        .GroupBy(x => x.Item.MaterialId)
                        .ToDictionary(
                            g => g.Key,
                            g => string.Join("||", g.Select(x => $"{x.Group}&&&{x.Item.Quantity}"))
                        );
                }

                return materialTags;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to fetch JSON file '{resourceName}': {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON format in file '{resourceName}': {ex.Message}", ex);
            }
        }

        private string GetOutputFullPath()
        {
            var folderPath = Path.Combine(_workingDir, _outputDirectory);
            var fullFilePath = Path.Combine(folderPath, _outputFilename);
            // Create the directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                _logger.LogInformation("System Directory created");
            }

            if (File.Exists(fullFilePath))
            {
                // Delete the existing file
                File.Delete(fullFilePath);
                _logger.LogInformation("Existing file deleted");
            }

            return fullFilePath;
        }

        private void AddDescriptionsToTop(ref Table descTable, List<DynValue> newDescList)
        {
            if (newDescList.Count == 0)
            {
                return;
            }

            // Add the existing descriptions to the new description list
            newDescList.AddRange(descTable.Values);

            // Clear the existing descriptions
            descTable.Clear();

            // ReApply the descriptions
            foreach (var desc in newDescList)
            {
                descTable.Append(desc);
            }
        }
    }
}
