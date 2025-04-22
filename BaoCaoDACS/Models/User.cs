using System.ComponentModel.DataAnnotations;

namespace BaoCaoDACS.Models
{
    public class User
    {
        public int UserID { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "User";
    }
}