namespace CodeAcademy.Models
{
    public class QuestionViewModel
    {
        public string QuestionText { get; set; }
        public int QuizId { get; set; }
        public int QuestionId { get; set; }
        public List<AnswerViewModel> Answers { get; set; }
        public int points {  get; set; }

    }
}
