﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CodeAcademy.Models;
using System.Security.Claims;

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
                    // Handle selected teacher
                    int selectedTeacherId = createCourse.TeacherId;
                    var selectedTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == selectedTeacherId);

                    // Create Course entity
                    var courseEntity = new Course
                    {
                        Title = createCourse.Title,
                        Description = createCourse.Description,
                        TeacherId = selectedTeacherId // Assign selected teacher
                    };

                    _context.Courses.Add(courseEntity);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                // Handle ModelState errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                }

                // Reload dropdown list for TeacherId
                ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "Name", createCourse.TeacherId);

                return View(createCourse);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                // Handle other exceptions if needed
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

                    return RedirectToAction("Index", "Home"); // Redirect to a different page after enrollment success
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine($"Error enrolling student: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while enrolling in the course. Please try again later.";
                    return View(course); // Return to the enrollment page with an error message
                }
            }
            else
            {
                TempData["ErrorMessage"] = "You are not authorized to enroll in courses.";
                return RedirectToAction("Index", "Home"); // Redirect to a different page for unauthorized access
            }
        }

        // Helper method to get the next available course ID
        private int GetNextCourseId()
        {
            int nextId = _context.CourseHasStudents.Max(p => (int?)p.Id) ?? 0;
            return nextId + 1;
        }
               
    }
}
