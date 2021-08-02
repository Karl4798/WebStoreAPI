using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebStore.Models
{
    [Table("Users", Schema = "dbo")]
    public class User : Auditing
    {
        [Key] public int Id { get; set; }
        [Required] public string Username { get; set; }
        [Required] public string Password { get; set; }
        public string Salt { get; set; }
        [NotMapped] public string Token { get; set; }
    }
}