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
        private int GenerateUniqueId()
        {
            int newId;
            do
            {
                newId = new Random().Next(1, int.MaxValue);
            } while (_context.Courses.Any(q => q.CourseId == newId));

            return newId;
        }
        // GET: Courses/Create
        public IActionResult Create()
        {
            var teacherId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == teacherId);

            if (teacher == null)
            {
                return NotFound("Teacher not found");
            }

            var createCourseModel = new CreateCourseModel
            {
                TeacherId = teacherId
            };

            ViewBag.TeacherId = new SelectList(new List<Teacher> { teacher }, "UserId", "Name", teacherId);

            return View(createCourseModel);
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
                        CourseId = GenerateUniqueId(),
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
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Title,Description,ImageUrl")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            // Fetch the existing entity from the database
            var existingCourse = await _context.Courses.FindAsync(id);
            if (existingCourse == null)
            {
                return NotFound();
            }
                // Update only the fields that need to be changed
                existingCourse.Title = course.Title;
                existingCourse.Description = course.Description;
                existingCourse.ImageUrl = course.ImageUrl; // Make sure to handle this field if it's supposed to be updated

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(existingCourse.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));

            // Return the view with the existing course details if the model state is invalid
            return View(existingCourse);
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
            // Εύρεση όλων των ενοτήτων που ανήκουν στο μάθημα
            var sections = await _context.CourseSections
                .Where(cs => cs.CourseId == id)
                .ToListAsync();

            // Εύρεση των quiz που σχετίζονται με τις ενότητες του μαθήματος
            var quizzes = await _context.Quizzes
                .Include(q => q.Questions) // Συμπερίληψη των ερωτήσεων που σχετίζονται με το quiz
                .Where(q => sections.Select(s => s.SectionId).Contains(q.SectionId))
                .ToListAsync();

            // Εύρεση των ερωτήσεων που ανήκουν στα quiz
            var questionIds = quizzes.SelectMany(q => q.Questions.Select(qu => qu.QuestionId)).ToList();

            // Εύρεση όλων των απαντήσεων που ανήκουν στις ερωτήσεις
            var answers = await _context.Answers
                .Where(a => questionIds.Contains(a.QuestionId))
                .ToListAsync();

            // Αφαίρεση όλων των απαντήσεων
            _context.Answers.RemoveRange(answers);

            // Αφαίρεση όλων των ερωτήσεων
            _context.Questions.RemoveRange(await _context.Questions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToListAsync());

            // Αφαίρεση όλων των quiz
            _context.Quizzes.RemoveRange(quizzes);

            // Αφαίρεση όλων των ενοτήτων (sections)
            _context.CourseSections.RemoveRange(sections);

            // Εύρεση όλων των εγγραφών των μαθητών που ανήκουν στο μάθημα
            var coursehasStudents = await _context.CourseHasStudents
                .Where(cs => cs.CourseId == id)
                .ToListAsync();

            // Αφαίρεση όλων των εγγραφών των μαθητών
            _context.CourseHasStudents.RemoveRange(coursehasStudents);

            // Εύρεση όλων των βαθμών που σχετίζονται με το μάθημα
            var studentIds = coursehasStudents.Select(cs => cs.StudentId).ToList();
            var grades = await _context.Grades
                .Where(g => studentIds.Contains(g.StudentId) && quizzes.Select(q => q.QuizId).Contains(g.QuizId))
                .ToListAsync();

            // Αφαίρεση όλων των βαθμών
            _context.Grades.RemoveRange(grades);

            // Αναζήτηση και αφαίρεση του μαθήματος
            var course = await _context.Courses.FindAsync(id);
            _context.Courses.Remove(course);

            // Αποθήκευση των αλλαγών στη βάση δεδομένων
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
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

            // Current logged-in user's userId
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If user is student
            if (!string.IsNullOrEmpty(userId) && User.IsInRole("Student"))
            {
                try
                {
                    // Check if the student is already enrolled in this course
                    var existingEnrollment = await _context.CourseHasStudents
                        .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == Int32.Parse(userId));

                    if (existingEnrollment != null)
                    {
                        // If already enrolled, show a message or handle as per your requirement
                        TempData["EnrollmentMessage"] = "You are already enrolled in this course.";
                        return RedirectToAction("CourseMainPage", new { id = id });
                    }

                    // If not already enrolled, proceed with enrollment
                    var courseHasStudent = new CourseHasStudent
                    {
                        CourseId = id,
                        StudentId = Int32.Parse(userId),
                        Id = GetNextCourseId() // Assuming GetNextCourseId() retrieves the next available id for the relationship
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


        // GET: Courses/CourseMainPage/id
        public async Task<IActionResult> CourseMainPage(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseSections)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var sections = await _context.CourseSections
                .Where(s => s.CourseId == id)
                .ToListAsync();

            ViewBag.CourseTitle = course.Title;
            ViewBag.CourseId = course.CourseId;

            return View(sections);
        }




        // Helper method to get the next available course ID
        private int GetNextCourseId()
        {
            int nextId = _context.CourseHasStudents.Max(p => (int?)p.Id) ?? 0;
            return nextId + 1;
        }
        }


    
}
