using System.ComponentModel.DataAnnotations.Schema;

namespace WebStore.Models
{
    [Table("Product", Schema = "dbo")]
    public class Product : Auditing
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string PhotoFileName { get; set; }
    }
}