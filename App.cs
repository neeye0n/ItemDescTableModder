using ItemDescTableModder.Models;
using ItemDescTableModder.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly string _workingDir;
        private readonly string _outputDirectory;
        private readonly string _outputFilename;
        private readonly string _instanceMatsTable;
        private readonly string _brewMatsTable;
        private readonly string _questMatsTable;
        private readonly string _cookinMatsTable;
        public App(ILogger<App> logger, ILuaTableHandler tableHandler, ILuaTableModifier tableModifier, string executableDirectory)
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


            _logger.LogInformation("Applying Item IDs, Brewing, Cooking, Quest and Instance Descriptions...");
            // Apply Item Ids
            foreach (var itemId in allItemIds)
            {
                // Order:
                // ItemId
                // Brewing
                // Cooking
                // Quest
                _tableModifier.ModifyItem(table, itemId, item =>
                 {
                     // Apply Item Id Description
                     _logger.LogDebug("Adding Item IDs...");
                     var newDescList = new List<DynValue>
                     {
                        DynValue.NewString($"^6666CCItem ID:^CC6600 {itemId}^000000")
                     };

                     // Apply Brewing Descriptions
                     if (brewingTags.TryGetValue(itemId, out _))
                     {
                         _logger.LogDebug("Adding Brewing related descriptions...");
                         newDescList.Add(DynValue.NewString($"^339900[Brewing Material]^000000"));
                     }

                     // Apply Cooking Descriptions
                     if (cookingTags.TryGetValue(itemId, out string cookingTag))
                     {
                         _logger.LogDebug("Adding Cooking related descriptions...");
                         var cookingInfos = cookingTag.Split(",");
                         if (cookingInfos.Length != 0)
                         {
                             newDescList.Add(DynValue.NewString($"^3F0099[Cooking Material]^000000"));
                             foreach (var cookingInfo in cookingInfos)
                             {
                                 var info = cookingInfo.Split("-");
                                 newDescList.Add(DynValue.NewString($"^3F0099{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));
                             }
                         }
                     }

                     // Apply Quest Information Descriptions
                     if (questTags.TryGetValue(itemId, out string questTag))
                     {
                         _logger.LogDebug("Adding Quest related descriptions...");
                         var questInfos = questTag.Split(",");
                         if (questInfos.Length != 0)
                         {
                             newDescList.Add(DynValue.NewString($"^ff9900[Quest Material]^000000"));
                             foreach (var questInfo in questInfos)
                             {
                                 var info = questInfo.Split("-");
                                 newDescList.Add(DynValue.NewString($"^ff9900{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));
                             }
                         }
                     }

                     // Apply Instance Information Descriptions
                     if (instanceTags.TryGetValue(itemId, out string instanceTag))
                     {
                         _logger.LogDebug("Adding Instance related descriptions...");
                         var instanceInfos = instanceTag.Split(",");
                         if (instanceInfos.Length != 0)
                         {
                             newDescList.Add(DynValue.NewString($"^990033[Instance Material]^000000"));
                             foreach (var instanceInfo in instanceInfos)
                             {
                                 var info = instanceInfo.Split("-");
                                 newDescList.Add(DynValue.NewString($"^990033{info[0].Trim()} - Amount: {info[1].Trim()}^000000"));
                             }
                         }

                     }

                     // Get Existing descriptions
                     Table descTable = item.Get("identifiedDescriptionName").Table;
                     var originalDescTable = descTable.Values;

                     // Add the existing descriptions to the new list
                     newDescList.AddRange(originalDescTable);

                     // Clear the existing descriptions
                     descTable.Clear();

                     // ReApply the descriptions
                     newDescList.ForEach(descriptions => descTable.Append(descriptions));
                 });
            }

            _logger.LogInformation("Tagging items...");
            // Apply Brew Prefix Tags
            foreach (var kvp in brewingTags)
            {
                _logger.LogDebug("Adding Brewing tags...");
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString("[Brew]" + startingSpace + displayName));
                });
            }

            // Apply Cooking Prefix Tags
            foreach (var kvp in cookingTags)
            {
                _logger.LogDebug("Adding Cooking tags...");
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString("[Cook]" + startingSpace + displayName));
                });
            }

            // Apply Quest Prefix Tags
            foreach (var kvp in questTags)
            {
                _logger.LogDebug("Adding Quest tags...");
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    var startingSpace = Regex.IsMatch(displayName, @"^(?:\[[^\]]*\])+") ? "" : " ";
                    item.Set("identifiedDisplayName", DynValue.NewString("[Quest]" + startingSpace + displayName));
                });
            }

            // Apply Suffix Tags
            foreach (var kvp in instanceTags)
            {
                _logger.LogDebug("Adding Suffix tags...");
                _tableModifier.ModifyItem(table, kvp.Key, item =>
                {
                    var displayName = item.Get("identifiedDisplayName").String;
                    item.Set("identifiedDisplayName", DynValue.NewString(displayName + " (" + kvp.Value + ")"));
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
    }
}
