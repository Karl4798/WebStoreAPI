using System.Text.Json.Serialization;

namespace WebStore.Models.DTOs
{
    public class CategoryDTO
    {
        [JsonIgnore]
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
    }
}