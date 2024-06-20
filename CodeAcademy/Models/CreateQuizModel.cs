using System.ComponentModel.DataAnnotations;

namespace CodeAcademy.Models
{
    public class CreateQuizModel
    {
        public int SectionId { get; set; }
        public string QuizName { get; set; }
        public int TotalPoints { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; }
        public int points { get; set; }

    }
}
