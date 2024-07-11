namespace CodeAcademy.Models
{
    public class DisplayIncorrectAnswersViewModel
    {
        public List<IncorrectAnswerViewModel> IncorrectAnswers { get; set; }
        public int TotalScore { get; set; }
        public double Percentage { get; set; }
        public int CourseId { get; set; }
    }

}
