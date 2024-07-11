using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CodeAcademy.Models;
using Microsoft.Data.SqlClient;
using X.PagedList;

namespace CodeAcademy.Controllers
{
    public class AdministratorsController : Controller
    {
        private readonly AcademyContext _context;

        public AdministratorsController(AcademyContext context)
        {
            _context = context;
        }

        // GET: Administrators
        public async Task<IActionResult> Index()
        {
            var academyContext = _context.Administrators.Include(a => a.User);
            return View(await academyContext.ToListAsync());
        }

        // GET: Administrators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administrators
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (administrator == null)
            {
                return NotFound();
            }

            return View(administrator);
        }

        // GET: Administrators/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Administrators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Name,Surname")] Administrator administrator)
        {
            if (ModelState.IsValid)
            {
                _context.Add(administrator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", administrator.UserId);
            return View(administrator);
        }

        // GET: Administrators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administrators.FindAsync(id);
            if (administrator == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", administrator.UserId);
            return View(administrator);
        }

        // POST: Administrators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Name,Surname")] Administrator administrator)
        {
            if (id != administrator.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(administrator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdministratorExists(administrator.UserId))
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
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", administrator.UserId);
            return View(administrator);
        }

        // GET: Administrators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administrators
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (administrator == null)
            {
                return NotFound();
            }

            return View(administrator);
        }

        // POST: Administrators/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var administrator = await _context.Administrators.FindAsync(id);
            if (administrator != null)
            {
                _context.Administrators.Remove(administrator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdministratorExists(int id)
        {
            return _context.Administrators.Any(e => e.UserId == id);
        }
        // GET: Admins/AssignRole
        public async Task<IActionResult> AssignRoleAsync(int? page)
        {
            var users = await _context.Users.ToListAsync(); // Example query to fetch all users

            int pageNumber = page ?? 1; // If no page number is specified, default to page 1
            int pageSize = 10; // Number of items per page

            // Convert the list of users to a paged list
            var pagedUsers = await users.ToPagedListAsync(pageNumber, pageSize);
            return View(pagedUsers);
        }

        // POST: Admins/AssignRole
        [HttpPost]
        public IActionResult AssignRole(string username, string newRole)
        {
            try
            {
                var user = _context.Users.SingleOrDefault(u => u.Username == username);
                if (user != null)
                {
                    //remove the user from the previous role table
                    RemoveFromPreviousRole(user);
                    
                    //assign new role to the user
                    user.Role = newRole;
                    _context.SaveChanges();
                    //TempData["SuccessMessage"] = "Role assigned successfully";
                    //return Ok(new { Message = "Role assigned successfully" });
                    
                    //add the record to the corresponding table based on the new role assigned to the user
                    switch (newRole.ToLower().Trim())
                    {
                        case "admin":
                            AddAdmin(user);
                            break;
                        case "teacher":
                            AddTeacher(user);
                            break;
                        case "student":
                            AddStudent(user);
                            break;
                        default:
                            TempData["ErrorMessage"] = "Invalid role assigned";
                            return BadRequest(new { Message = "Invalid role assigned" });
                    }

                    TempData["SuccessMessage"] = "Role assigned and record added successfully";
                    return Ok(new { Message = "Role assigned and record added successfully" });
                }
                TempData["ErrorMessage"] = "User not found";
                return NotFound(new { Message = "User not found" });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlException && sqlException.Number == 2627)
                {
                    TempData["ErrorMessage"] = "Error: Duplicate record already exists in the table.";
                    return BadRequest(new { Message = "Error: Duplicate record already exists in the table." });
                }
                else
                {
                    TempData["ErrorMessage"] = "Error: Internal Server Error while processing the request.";
                    return StatusCode(500, new { Message = "Internal Server Error" });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return StatusCode(500, new { Message = "Internal Server Error" });
            }
        }

        private void RemoveFromPreviousRole(User user)
        {
            switch (user.Role?.ToLower().Trim())
            {
                case "admin":
                    var admin = _context.Administrators.SingleOrDefault(a => a.UserId == user.UserId);
                    if (admin != null) _context.Administrators.Remove(admin);
                    break;
                case "teacher":
                    var teacher = _context.Teachers.SingleOrDefault(t => t.UserId == user.UserId);
                    if (teacher != null) _context.Teachers.Remove(teacher);
                    break;
                case "student":
                    var student = _context.Students.SingleOrDefault(s => s.UserId == user.UserId);
                    if (student != null) _context.Students.Remove(student);
                    break;
            }
            _context.SaveChanges();
        }

        private void AddTeacher(User user)
        {
            var teacher = new Teacher
            {
                UserId = user.UserId,
                // Set other necessary properties
            };
            _context.Teachers.Add(teacher);
            _context.SaveChanges();
        }

        private void AddAdmin(User user)
        {
            var admin = new Administrator
            {
                UserId = user.UserId,
                // Set other necessary properties
            };
            _context.Administrators.Add(admin);
            _context.SaveChanges();
        }

        private void AddStudent(User user)
        {
            var student = new Student
            {
                UserId = user.UserId,
                // Set other necessary properties
            };
            _context.Students.Add(student);
            _context.SaveChanges();
        }
    }
}
