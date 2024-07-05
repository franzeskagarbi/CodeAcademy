using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeAcademy.Models
{
    public class EditProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Surname { get; set; }

        public string Role { get; set; } // which table to update

        [RegularExpression(@"^\d{10}$", ErrorMessage = "Please enter a valid 10-digit telephone number")]
        public string Telephone { get; set; } // for teachers (optional)
    }
}
