using CodeAcademy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AcademyContext _context;

        public TeacherController(AcademyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ViewStudents()
        {
            // Fetch students enrolled in courses
            var enrolledStudents = await _context.CourseHasStudents
                .Include(chs => chs.Course)
                .Include(chs => chs.Student)
                .ToListAsync();

            return View(enrolledStudents);
        }
    }
}