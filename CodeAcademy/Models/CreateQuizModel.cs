namespace CodeAcademy.Models
{
    public class CreateQuizModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string SectionName { get; set; }
        public string Description { get; set; }
        public string QuizName { get; set; }
        public int TotalPoints { get; set; }
    }
}
