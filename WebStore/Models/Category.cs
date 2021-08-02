using System.ComponentModel.DataAnnotations.Schema;

namespace WebStore.Models
{
    [Table("Category", Schema = "dbo")]
    public class Category : Auditing
    {
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
    }
}