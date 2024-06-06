using Microsoft.Build.Framework;

namespace CodeAcademy.Models
{
    public partial class CreateCourseModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int TeacherId { get; set; } // Assuming TeacherId is an integer representing the teacher's ID

        // If you need additional properties for validation or data binding, add them here
    }

}
