using CodeAcademy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CodeAcademy.Controllers
{
    public class GradesController : Controller
    {
        private readonly AcademyContext _context;

        public GradesController(AcademyContext context)
        {
            _context = context;
        }
        public IActionResult TeacherGradesView()
        {
                var totalScores = _context.Grades
                                        .Include(g => g.Student)
                                        .Include(g => g.Quiz)
                                            .ThenInclude(q => q.Section)
                                                .ThenInclude(sec => sec.Course) // Include necessary navigation properties
                                        .GroupBy(g => new { g.StudentId, g.Student.Name,g.Student.Surname, g.Quiz.Section.Course.Title })
                                        .Select(g => new TotalScoreViewModel
                                        {
                                            StudentId = g.Key.StudentId,
                                            StudentName = g.Key.Name,
                                            StudentSurname = g.Key.Surname,
                                            CourseTitle = g.Key.Title,
                                            TotalScore = g.Sum(x => x.Score)
                                        })
                                        .ToList();

                return View(totalScores);
        }
        public async Task<IActionResult> StudentGradesView()
        {
            // Get the logged-in student's ID
            var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Retrieve the student's grades, quizzes, sections, and courses
            var studentGrades = await _context.Grades
                .Include(g => g.Quiz)
                .ThenInclude(q => q.Section)
                .ThenInclude(s => s.Course)
                .Where(g => g.StudentId == studentId)
                .GroupBy(g => new { g.Quiz.Section.Course.Title, g.Quiz.Section.SectionName, g.Quiz.QuizName })
                .Select(g => new StudentGradesViewModel
                {
                    CourseTitle = g.Key.Title,
                    SectionName = g.Key.SectionName,
                    QuizName = g.Key.QuizName,
                    Score = g.Sum(x => x.Score),
                    TotalCourseScore = _context.Grades
                        .Include(gr => gr.Quiz)
                        .ThenInclude(qz => qz.Section)
                        .Where(gr => gr.StudentId == studentId && gr.Quiz.Section.Course.Title == g.Key.Title)
                        .Sum(gr => gr.Score)
                })
                .ToListAsync();

            return View(studentGrades);
        }
    }
}
