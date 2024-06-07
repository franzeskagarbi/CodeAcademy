using CodeAcademy.Models;

namespace CodeAcademy.ViewModels
{
    public class CourseViewModel
    {
        public CreateCourseModel CreateCourseModel { get; set; }

        public IEnumerable<Course> Courses { get; set; }
        public Course Course { get; set; }
        public bool IsEnrolled { get; set; }
    }
}
