namespace CodeAcademy.Models
{
    public class GradeViewModel
    {
            public int GradeId { get; set; }
            public int StudentId { get; set; }
            public int QuizId { get; set; }
            public int Score { get; set; }

            public string StudentName { get; set; }
            public string CourseTitle { get; set; }

    }
}
