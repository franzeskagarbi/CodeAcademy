using System.Security;

namespace CodeAcademy.Models
{
    public class TotalScoreViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentSurname {  get; set; }
        public string CourseTitle { get; set; }
        public int TotalScore { get; set; }
    }
}
