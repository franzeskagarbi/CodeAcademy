namespace CodeAcademy.Models
{
    public class IncorrectAnswerViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string SelectedAnswerText { get; set; } 
        public string CorrectAnswerText { get; set; }
        public List<AnswerViewModel> AnswerOptions { get; set; }
    }

}
