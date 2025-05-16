using Newtonsoft.Json;

namespace ItemDescTableModder.Models
{
    public class Config
    {
        [JsonProperty(Required = Required.Always)]
        public string ItemIdDescTextColor { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ItemIdDescValueColor { get; set; }
        [JsonProperty(Required = Required.Always)]
        public TaggingConfig BrewingConfig { get; set; }
        [JsonProperty(Required = Required.Always)]
        public TaggingConfig CookingConfig { get; set; }
        [JsonProperty(Required = Required.Always)]
        public TaggingConfig QuestConfig { get; set; }
        [JsonProperty(Required = Required.Always)]
        public TaggingConfig InstanceConfig { get; set; }

        [JsonConstructor]
        public Config() { }

    }

    public class TaggingConfig
    {
        public required int EnableTags { get; set; }
        public required int EnableDescriptions { get; set; }
        public required string TagText { get; set; }
        public required string DescriptionHeaderColor { get; set; }
        public required string DescriptionRowsColor { get; set; }

    }
}
