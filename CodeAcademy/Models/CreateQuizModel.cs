using System.ComponentModel.DataAnnotations;

namespace CodeAcademy.Models
{
    public class CreateQuizModel
    {
        public int QuizId { get; set; }
        public int SectionId { get; set; }

        [Required]
        [StringLength(200)]
        public string QuizName { get; set; } = null!;

        [Required]
        public int TotalPoints { get; set; }

        public List<Question> Questions { get; set; } = new List<Question>();
    }
}
