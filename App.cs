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
        void ProcessFile(string filePath);
    }

    public class App : IApp
    {
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

        public App(ILogger<App> logger, ILuaTableHandler tableHandler, ILuaTableModifier tableModifier, string executableDirectory, IOptions<Config> config)
        {
            _logger = logger;
            _tableHandler = tableHandler;
            _tableModifier = tableModifier;
            _outputFilename = "itemInfo_EN.lua";
            _outputDirectory = "System";
            _instanceMatsTable = "ItemDescTableModder.Resources.InstanceMatsTable.json";
            _brewMatsTable = "ItemDescTableModder.Resources.BrewingMatsTable.json";
            _questMatsTable = "ItemDescTableModder.Resources.QuestMatsTable.json";
            _cookinMatsTable = "ItemDescTableModder.Resources.CookingMatsTable.json";

            _workingDir = executableDirectory;
            _config = config.Value;
        }

        public void ProcessFile(string filePath)
        {
            _logger.LogInformation("Starting to process the file...");

            var table = _tableHandler.LoadFile(filePath, "tbl");
            var allItemIds = _tableModifier.GetItemIds(table);
            var instanceTags = GenerateMaterialTags(_instanceMatsTable);
            var brewingTags = GenerateMaterialTags(_brewMatsTable);
            var questTags = GenerateMaterialTags(_questMatsTable);
            var cookingTags = GenerateMaterialTags(_cookinMatsTable);

            // Order of Precedence:
            // ItemId
            // Brewing
            // Cooking
            // Quest
            // Instance

            // Apply Instance Suffix
            _logger.LogInformation("Applying Instance descriptions and tags...");
            foreach (var kvp in instanceTags)
            {
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    item.Set("identifiedDisplayName", DynValue.NewString(displayName + " (" + kvp.Value + ")"));

                    var descriptionTable = item.Get("identifiedDescriptionName").Table;
                    var instanceInfos = kvp.Value.Split(",");
                    if (instanceInfos.Length != 0)
                    {
                        List<DynValue> newDescList = [DynValue.NewString($"^990033[Instance Material]^000000")];
                        foreach (var instanceInfo in instanceInfos)
                        {
                            var info = instanceInfo.Split("-");
                            newDescList.Add(DynValue.NewString($"^990033{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));
                        }
                        AddDescriptionsToTop(ref descriptionTable, newDescList);
                    }
                });
            }

            // Apply Quest Prefix
            _logger.LogInformation("Applying Quest descriptions and tags...");
            foreach (var kvp in questTags)
            {
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.QuestTagText}]" + startingSpace + displayName));

                    var descriptionTable = item.Get("identifiedDescriptionName").Table;
                    var questInfos = kvp.Value.Split(",");
                    if (questInfos.Length != 0)
                    {
                        List<DynValue> newDescList = [DynValue.NewString($"^{_config.QuestDescTextColor}[Quest Material]^000000")];
                        foreach (var questInfo in questInfos)
                        {
                            var info = questInfo.Split("-");
                            newDescList.Add(DynValue.NewString($"^{_config.QuestDescTextColor}{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));

                        }
                        AddDescriptionsToTop(ref descriptionTable, newDescList);
                    }
                });
            }

            // Apply Cooking Prefix
            _logger.LogInformation("Applying Cooking descriptions and tags...");
            foreach (var kvp in cookingTags)
            {
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.CookingTagText}]" + startingSpace + displayName));

                    var descriptionTable = item.Get("identifiedDescriptionName").Table;
                    var cookingInfos = kvp.Value.Split(",");
                    if (cookingInfos.Length != 0)
                    {
                        List<DynValue> newDescList = [DynValue.NewString($"^{_config.CookingDescTextColor}[Cooking Material]^000000")];
                        foreach (var cookingInfo in cookingInfos)
                        {
                            var info = cookingInfo.Split("-");
                            newDescList.Add(DynValue.NewString($"^{_config.CookingDescTextColor}{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));
                        }
                        AddDescriptionsToTop(ref descriptionTable, newDescList);
                    }
                });
            }

            // Apply Brew Prefix
            _logger.LogInformation("Applying Brewing descriptions and tags...");
            foreach (var kvp in brewingTags)
            {
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString($"[{_config.BrewingTagText}]" + startingSpace + displayName));

                    var descriptionTable = item.Get("identifiedDescriptionName").Table;
                    AddDescriptionsToTop(ref descriptionTable, [
                        DynValue.NewString($"^{_config.BrewingDescTextColor}[Brewing Material]^000000")
                    ]);
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
        }

        private Dictionary<int, string> GenerateMaterialTags(string resourceName)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException("Embedded resource not found.");
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
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
                    g => string.Join(", ", g.Select(x => $"{x.Group} - {x.Item.Quantity}"))
                );
            }

            return materialTags;
        }

        private string GetOutputFullPath()
        {
            var folderPath = Path.Combine(_workingDir, _outputDirectory);
            var fullFilePath = Path.Combine(folderPath, _outputFilename);
            // Create the directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                _logger.LogInformation("Directory created: {folderPath}", folderPath);
            }

            if (File.Exists(fullFilePath))
            {
                // Delete the existing file
                File.Delete(fullFilePath);
                _logger.LogInformation("Existing file deleted: {fullFilePath}", fullFilePath);
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
