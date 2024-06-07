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
        public int TeacherId { get; set; }

        public IFormFile Image { get; set; } // file upload
    }

}
