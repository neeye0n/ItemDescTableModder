using System.Text.Json.Serialization;

namespace ItemDescTableModder.Models
{
    public class MaterialInfo
    {
        [JsonPropertyName("matId")]
        public int MaterialId { get; set; }
        [JsonPropertyName("matName")]
        public string MaterialName { get; set; }
        [JsonPropertyName("qty")]
        public int Quantity { get; set; }

        public MaterialInfo()
        {
            MaterialName = string.Empty;
        }
    }
}
