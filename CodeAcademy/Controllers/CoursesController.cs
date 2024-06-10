using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CodeAcademy.Models;
using System.Security.Claims;
using CodeAcademy.ViewModels;
using static System.Collections.Specialized.BitVector32;

namespace CodeAcademy.Controllers
{
    public class CoursesController : Controller
    {
        private readonly AcademyContext _context;

        public CoursesController(AcademyContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            var academyContext = _context.Courses.Include(c => c.Teacher);
            return View(await academyContext.ToListAsync());
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            CreateCourseModel courseModel = new CreateCourseModel();
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "Name");
            return View(courseModel);
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseModel createCourse)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string imageUrl = null;

                    if (createCourse.Image != null && createCourse.Image.Length > 0)
                    {
                        // Save the file to wwwroot/images
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", createCourse.Image.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await createCourse.Image.CopyToAsync(stream);
                        }
                        imageUrl = "/images/" + createCourse.Image.FileName;
                    }

                    // Retrieve the selected teacher
                    int selectedTeacherId = createCourse.TeacherId;
                    var selectedTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == selectedTeacherId);

                    if (selectedTeacher == null)
                    {
                        ModelState.AddModelError("TeacherId", "Selected teacher does not exist.");
                        ViewBag.TeacherId = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);
                        return View(createCourse);
                    }

                    // Create a new Course entity
                    var courseEntity = new Course
                    {
                        CourseId = createCourse.Id,
                        Title = createCourse.Title,
                        Description = createCourse.Description,
                        TeacherId = selectedTeacherId,
                        ImageUrl = imageUrl
                    };

                    _context.Courses.Add(courseEntity);

                    // Log the state of the entity
                    var addedEntity = _context.Entry(courseEntity).State;
                    Console.WriteLine("Entity State after adding: " + addedEntity); // Should be 'Added'

                    // Save changes to the database
                    await _context.SaveChangesAsync();
                    Console.WriteLine("SaveChangesAsync was called successfully.");

                    // Ensure the entity was saved
                    var savedEntity = await _context.Courses.FirstOrDefaultAsync(c => c.Title == createCourse.Title && c.TeacherId == selectedTeacherId);
                    if (savedEntity == null)
                    {
                        Console.WriteLine("Error: The course entity was not saved.");
                        ModelState.AddModelError("", "The course entity was not saved to the database.");
                        ViewBag.TeacherId = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);
                        return View(createCourse);
                    }
                    else
                    {
                        Console.WriteLine("Success: The course entity was saved.");
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Log ModelState errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine("ModelState Error: " + error.ErrorMessage);
                    }
                }

                // Reload dropdown list for TeacherId in case of validation failure
                ViewBag.TeacherId = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);
                return View(createCourse);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("DbUpdateException: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                    Console.WriteLine("Inner Exception Stack Trace: " + ex.InnerException.StackTrace);
                }
                // Handle other exceptions if needed
                ViewBag.TeacherId = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);
                return View(createCourse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                    Console.WriteLine("Inner Exception Stack Trace: " + ex.InnerException.StackTrace);
                }
                // Handle other exceptions if needed
                ViewBag.TeacherId = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);
                return View(createCourse);
            }
        }



        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId", course.TeacherId);
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Title,TeacherId,Description")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId", course.TeacherId);
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        // GET: Courses/Enroll/id
        [HttpGet]
        public async Task<IActionResult> Enroll(int id)
        {
            // Fetch the course details from the database
            var course = await _context.Courses.FindAsync(id);

            // Check if the course exists
            if (course == null)
            {
                // If the course does not exist, return a not found error
                return NotFound();
            }

            // Pass the course model to the view
            return View(course);
        }

        // POST: Courses/Enroll/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollPost(int id)
        {
            // Fetch the course details from the database
            var course = await _context.Courses.FindAsync(id);

            // Check if the course exists
            if (course == null)
            {
                // If the course does not exist, return a not found error
                return NotFound();
            }

            //current logged-in user's userId
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //if user is student
            if (!string.IsNullOrEmpty(userId) && User.IsInRole("Student"))
            {
                try
                {
                    var courseHasStudent = new CourseHasStudent
                    {
                        CourseId = id,
                        StudentId = Int32.Parse(userId),
                        Id = GetNextCourseId()
                    };
                    _context.CourseHasStudents.Add(courseHasStudent);
                    await _context.SaveChangesAsync();

                    TempData["EnrollmentMessage"] = "You have successfully enrolled in the course.";

                    return RedirectToAction("CourseMainPage", new { id = id }); // Redirect to the CourseMainPage after enrollment success
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine($"Error enrolling student: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while enrolling in the course. Please try again later.";
                    return RedirectToAction("Enroll", new { id = id }); 
                }
            }
            else
            {
                TempData["ErrorMessage"] = "You are not authorized to enroll in courses.";
                return RedirectToAction("Index", "Home"); // Redirect to a different page for unauthorized access
            }
        }

        public IActionResult CourseMainPage(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // Helper method to get the next available course ID
        private int GetNextCourseId()
        {
            int nextId = _context.CourseHasStudents.Max(p => (int?)p.Id) ?? 0;
            return nextId + 1;
        }
        }


    
}
