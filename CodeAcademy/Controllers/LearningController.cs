using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CodeAcademy.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Controllers
{
    public class LearningController : Controller
    {
        private readonly AcademyContext _context;

        public LearningController(AcademyContext context)
        {
            _context = context;
        }

        private int GetStudentId()
        {
            // Replace with your method to get the current student's ID
            return 1; // Example student ID
        }
        

        private async Task<int> GetStudentScore(int studentId, int quizId)
        {
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.StudentId == studentId && g.QuizId == quizId);
            return grade?.Score ?? 0;
        }

        private async Task<IActionResult> GetLearningPath(int courseId, int totalScore, string sectionLevel, string viewName)
        {
            var course = await _context.Courses
                .Include(c => c.CourseSections)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                Console.WriteLine("courseid not correctly passed");
                return NotFound();
            }

            List<CourseSection> sections = await _context.CourseSections
                .Where(s => s.CourseId == courseId && s.SectionLevel == sectionLevel)
                .OrderBy(s => s.SectionName)
                .ToListAsync();

            ViewBag.CourseTitle = course.Title;
            ViewBag.CourseId = course.CourseId;
            ViewBag.totalScore = totalScore;

            return View(viewName, sections);
        }

        // GET: Learning/MediumLearningPath
        public async Task<IActionResult> MediumLearningPath(int id, int totalScore)
        {
            ViewBag.totalScore = totalScore;
            return await GetLearningPath(id, totalScore, "medium", "MediumLearningPath");
        }
        // GET: Learning/MediumPlusLearningPath
        public async Task<IActionResult> MediumPlusLearningPath(int id, int totalScore)
        {
            ViewBag.totalScore = totalScore;
            return await GetLearningPath(id, totalScore, "medium+", "MediumPlusLearningPath");
        }

        // GET: Learning/AdvancedLearningPath
        public async Task<IActionResult> AdvancedLearningPath(int id, int totalScore)
        {
            ViewBag.totalScore = totalScore;
            return await GetLearningPath(id, totalScore, "advanced", "AdvancedLearningPath");
        }
        // GET: Learning/AdvancedLearningPath
        public async Task<IActionResult> BasicMinusLearningPath(int id, int totalScore)
        {
            ViewBag.totalScore = totalScore;
            return await GetLearningPath(id, totalScore, "-basic", "BasicMinusLearningPath");
        }

    }
}
