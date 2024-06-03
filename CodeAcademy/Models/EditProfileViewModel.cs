using System.ComponentModel.DataAnnotations;

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

        public string Role { get; set; } // Role is needed to determine which table to update
    }
}
