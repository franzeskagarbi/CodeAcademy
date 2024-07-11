using CodeAcademy.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace CodeAcademy.Controllers
{
    public class SectionsController : Controller
    {
        private readonly AcademyContext _context;

        public SectionsController(AcademyContext context)
        {
            _context = context;
        }
        // GET: Sections/Create
        public IActionResult CreateSections(int courseId)
        {
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course != null)
            {
                ViewData["CourseTitle"] = course.Title;
            }
            else
            {
                ViewData["CourseTitle"] = "Unknown Course"; // Provide a default value if course title is not found
            }
            var model = new CreateSectionModel
            {
                CourseId = courseId
            };
            ViewData["CourseId"] = courseId; // Optional if you want to use ViewData in the view
            return View(model);
        }


        // POST: Sections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSections(CreateSectionModel createSection, int courseId)
        {
            // Log courseId for debugging
            try
            {
  
                    var sectionEntity = new CourseSection
                    {
                        CourseId = courseId,
                        SectionId = GenerateUniqueSectionId(),
                        SectionName = createSection.SectionName,
                        Description = createSection.Description,
                        SectionLevel = createSection.SectionLevel

                    };

                    _context.CourseSections.Add(sectionEntity);

                    // Log the state of the entity
                    var addedEntity = _context.Entry(sectionEntity).State;
                    Console.WriteLine("Entity State after adding: " + addedEntity); // Should be 'Added'

                    // Save changes to the database
                    await _context.SaveChangesAsync();
                    Console.WriteLine("SaveChangesAsync was called successfully.");

                    // Ensure the entity was saved
                    var savedEntity = await _context.CourseSections.FirstOrDefaultAsync(c => c.SectionName == createSection.SectionName);
                    if (savedEntity == null)
                    {
                        Console.WriteLine("Error: The course entity was not saved.");
                        ModelState.AddModelError("", "The course entity was not saved to the database.");
                        return View(createSection);
                    }
                    else
                    {
                        Console.WriteLine("Success: The course entity was saved.");
                    }
            }
            catch (Exception ex)
            {
                // Log the exception details
                ModelState.AddModelError("", "An error occurred while saving the section. Please try again.");
                return View(createSection);
            }
                    return RedirectToAction(nameof(ViewSections), new { courseId = courseId });


    }

    // GET: Sections/ViewSections
    public async Task<IActionResult> ViewSections(int courseId)
        {
            ViewBag.CourseId = courseId;

            var course = await _context.Courses
                    .Include(c => c.CourseSections)
                        .ThenInclude(s => s.Quizzes)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId); if (course == null)
            {
                return NotFound();
            }

            var courseSections = await _context.CourseSections
                .Where(cs => cs.CourseId == courseId)
                .ToListAsync();

            ViewData["CourseTitle"] = course.Title; // Pass course title to view

            return View(courseSections);
        }

        private int GenerateUniqueSectionId()
        {
             int newId;
             do
             {
              newId = new Random().Next(1, int.MaxValue);
             } while (_context.CourseSections.Any(s => s.SectionId == newId));

             return newId;
        }

        [HttpGet]
        public IActionResult Guidelines()
        {
            return View();
        }

        // GET: Sections/Edit/
        public async Task<IActionResult> EditSections(int? id, int courseId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.CourseSections.FindAsync(id);
            if (section == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = section.CourseId;

            return View(section);
        }

        // POST: Sections/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSections(int id, [Bind("SectionId,CourseId,SectionName,Description,SectionLevel")] CourseSection section)
        {
            if (id != section.SectionId)
            {
                return NotFound();
            }

                try
                {
                    _context.Update(section);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SectionExists(section.SectionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // Redirect to ViewSections with the courseId after a successful edit
                return RedirectToAction(nameof(ViewSections), new { courseId = section.CourseId });
        
            // If model state is invalid, return to the edit view with the section model
            return View(section);
        }

        private bool SectionExists(int id)
        {
            return _context.CourseSections.Any(e => e.SectionId == id);
        }
        // GET: Sections/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.CourseSections
                .FirstOrDefaultAsync(m => m.SectionId == id);
            if (section == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = section.CourseId;

            return View(section);
        }

        // POST: Sections/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Εύρεση των quiz που σχετίζονται με την ενότητα
            var quizzes = await _context.Quizzes
                .Include(q => q.Questions) // Συμπερίληψη των ερωτήσεων που σχετίζονται με το quiz
                .Where(q => q.SectionId == id)
                .ToListAsync();

            // Εύρεση των ερωτήσεων που ανήκουν στα quiz και σχετίζονται με την ενότητα
            var questionIds = quizzes.SelectMany(q => q.Questions.Select(qu => qu.QuestionId)).ToList();

            // Διαγραφή των απαντήσεων που ανήκουν στις ερωτήσεις
            var answers = await _context.Answers
                .Where(a => questionIds.Contains(a.QuestionId))
                .ToListAsync();

            var quizIds = quizzes.Select(q => q.QuizId).ToList();

            // διαγραφή απαντήσεων 
            _context.Answers.RemoveRange(answers);

            // Διαγραφή των ερωτήσεων
            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToListAsync();

            // ευρεση student_asnwers που ανηκουν στο quiz που ανηκει στο section που θελουμε να διαγραψουμε
            var studentAnswers = await _context.StudentAnswers
                .Where(sa => quizIds.Contains((int)sa.QuizId))
                .ToListAsync();

            // διαγραφη student answers
            _context.StudentAnswers.RemoveRange(studentAnswers);

            _context.Questions.RemoveRange(questions);

            // Διαγραφή των quiz
            _context.Quizzes.RemoveRange(quizzes);

            // Εύρεση των βαθμών που σχετίζονται με τα κουίζ της ενότητας
            //var quizIds = quizzes.Select(q => q.QuizId).ToList();
            var grades = await _context.Grades
                .Where(g => quizIds.Contains(g.QuizId))
                .ToListAsync();
            _context.Grades.RemoveRange(grades);

           // Εύρεση της ενότητας
            var section = await _context.CourseSections.FindAsync(id);
            if (section != null)
            {
                _context.CourseSections.Remove(section);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Internal server error while deleting the section.");
            }

            var courseId = section.CourseId;
            return RedirectToAction(nameof(ViewSections), new { courseId });
        }



    }
}
