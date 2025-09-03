using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TodoAPI.Models
{
    
    public class Tasks
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
