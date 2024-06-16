namespace CodeAcademy.Models
{
    public class CreateAnswerModel
    {
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }
        public int IsCorrect { get; set; } 
    }
}
