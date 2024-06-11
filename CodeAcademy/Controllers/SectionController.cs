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
                        Description = createSection.Description
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

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
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
        public async Task<IActionResult> EditSections(int id, [Bind("SectionId,CourseId,SectionName,Description")] CourseSection section)
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
            var section = await _context.CourseSections.FindAsync(id);
            var courseId = section.CourseId;
            if (section != null)
            {
                _context.CourseSections.Remove(section);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewSections), new { courseId });
        }

    }
}
