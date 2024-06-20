namespace CodeAcademy.Models
{
    public class CreateQuestionModel
    {
        public int QuizId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } 

        public int Points { get; set; }
    }
}
