using System.ComponentModel.DataAnnotations.Schema;

namespace CodeAcademy.Models
{
    public class QuizSubmissionViewModel
    {
        public int QuizId { get; set; }

        public List<Question> Questions { get; set; }
        public List<AnswerSubmission> StudentAnswers { get; set; }
    }
}
