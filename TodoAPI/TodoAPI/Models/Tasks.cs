using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TodoAPI.Enum;
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
        public TasksStatus Status { get; set; } = TasksStatus.active;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
