namespace CodeAcademy.Models
{
    public class ViewQuizModel
    {
        public int QuizId { get; set; }
        public int SectionId { get; set; }
        public string QuizName { get; set; }
        public int TotalPoints { get; set; }
        public List<Question> Questions { get; set; }
    }
}
