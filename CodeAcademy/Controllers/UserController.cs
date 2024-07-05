using CodeAcademy.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using X.PagedList;
using NuGet.DependencyResolver;

namespace CodeAcademy.Controllers
{
    public class UserController : Controller
    {
        private readonly AcademyContext _context;
        private readonly ISession _session;
        /*public IActionResult Index()
        {
            return View();
        } */

        public UserController(AcademyContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _session = httpContextAccessor.HttpContext.Session;
        }

        // GET: Users
        public async Task<IActionResult> Index(int? page, string? search)
        {
            ViewData["CurrentFilter"] = search;
            var users = from u in _context.Users
                        select u;
            if (!String.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.Username.Contains(search));
            }

            //users = users.OrderBy(c => c.Username);

            //users = await _context.Users.ToListAsync();
            // Pagination for users
            if (page != null && page < 1)
            {
                page = 1;
            }

            int PageSize = 10;
            var usersData = await users.ToPagedListAsync(page ?? 1, PageSize);
            return View(usersData);
        }

        public IActionResult Login()
        {
            return View();
        }

        /* [HttpPost]
         [ValidateAntiForgeryToken]
         public IActionResult Login(LoginViewModel objUser)
         {
             if (ModelState.IsValid)
             {
                 var obj = _context.Users
                     .FirstOrDefault(a => a.Username.Equals(objUser.Username) && a.Password.Equals(objUser.Password));

                 if (obj != null)
                 {
                     _session.SetString("UserID", obj.UserId.ToString());
                     _session.SetString("UserName", obj.Username);
                     Console.WriteLine("redirect");
                     return RedirectToAction("UserDashboard");
                 }
                 else
                 {
                     ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                 }
             }
             else
             {
                 foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                 {
                     Console.WriteLine(error.ErrorMessage);
                 }
             }
             return View(objUser);
         }*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel objUser)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the user from the database based on the provided username
                var user = _context.Users.SingleOrDefault(u => u.Username == objUser.Username);

                if (user != null)
                {
                    string name = null;
                    string surname = null;
                    // Validate the entered password with the stored hashed password and salt
                    if (!string.IsNullOrEmpty(objUser.Password) && ValidatePassword(objUser.Password, user.Password, user.Salt))
                    {
                        switch (user.Role?.ToLower()?.Trim())
                        {
                            case "admin":
                                var admin = _context.Administrators.FirstOrDefault(a => a.UserId == user.UserId);
                                name = admin?.Name;
                                surname = admin?.Surname;
                                break;
                            case "student":
                                var student = _context.Students.FirstOrDefault(s => s.UserId == user.UserId);
                                name = student?.Name;
                                surname = student?.Surname;
                                break;
                            case "teacher":
                                var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                                name = teacher?.Name;
                                surname = teacher?.Surname;
                                break;
                            case "none":
                                TempData["RoleError"] = "Your account does not have an assigned role. Please contact the administrators of the platform.";
                                return RedirectToAction("Login");
                        }

                        if (name == "tba" || surname == "tba")
                        {
                            // Redirect to the edit form to complete the profile
                            return RedirectToAction("EditProfile", new { userId = user.UserId });
                        }
                        // Passwords match, login successful
                        //_session.SetString("UserID", user.UserId.ToString());
                        //_session.SetString("UserName", user.Username);

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Role, user.Role)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        

                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity)).Wait();


                        Console.WriteLine("redirect");
                        return RedirectToAction("Index", "Home");
                    }
                }

                // Incorrect username or password, return to login view
                ModelState.AddModelError(string.Empty, "Invalid username or password");
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }
            return View(objUser);
        }

        /*public IActionResult UserDashBoard()
        {
            var userId = HttpContext.Session.GetString("UserID");
            var userName = HttpContext.Session.GetString("UserName");
            if (userId != null)
            {
                Console.WriteLine($"User {userId} ({userName}) is authenticated. Showing dashboard.");
                ViewBag.UserName = userName; // Pass the username to the view
                return View();
            }
            else
            {
                Console.WriteLine("User is not authenticated. Redirecting to login.");
                return RedirectToAction("Login");
            }
        } */

        public IActionResult UserDashboard()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (role == "none")
                {
                    TempData["RoleError"] = "Your account does not have an assigned role. Please contact the administrators of the platform.";
                    return RedirectToAction("Login");
                }

                ViewBag.UserName = User.Identity.Name;
                return View();
            }
            return RedirectToAction("Login");
        }

       
        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Username,Email,Password")] User user)
        {
            user.Role = "none";

            if (ModelState.IsValid)
            {
                // Set other properties
                user.UserId = GetNextId();
                // Generate a random salt
                user.Salt = Convert.ToBase64String(GenerateSalt());

                // Hash the password using the generated salt
                user.Password = HashPassword(user.Password, user.Salt);
                // Add the user to the context
                _context.Add(user);

                // Save changes to the database
                try
                {
                    await _context.SaveChangesAsync();

                    if (user.Role == "none")
                    {
                        TempData["RoleError"] = "Your account has been created, but no role has been assigned yet. Please contact the administrators of the platform.";
                        return RedirectToAction("Login");
                    }
                    // Set session parameters
                    _session.SetString("UserID", user.UserId.ToString());
                    _session.SetString("UserName", user.Username);
                    return RedirectToAction(nameof(UserDashboard));
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine(ex.Message);
                    // Handle the exception as needed
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // Log the error
                    Console.WriteLine(error.ErrorMessage);
                }
            }

            return View(user);
        }
             

        private int GetNextId()
        {
            // Logic to get the next available ID, for example, querying the database or using a counter
            int nextId = _context.Users.Max(m => (int?)m.UserId) ?? 0;
            return nextId + 1;
        }

        private string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);

            using (var sha256 = new SHA256Managed())
            {
                byte[] combinedBytes = Encoding.UTF8.GetBytes(password).Concat(saltBytes).ToArray();
                byte[] hashedPasswordBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashedPasswordBytes);
            }
        }

        // methods for hashing password before saving it into the db
        private bool ValidatePassword(string enteredPassword, string storedHashedPassword, string salt)
        {
            string hashedEnteredPassword = HashPassword(enteredPassword, salt);
            return string.Equals(hashedEnteredPassword, storedHashedPassword, StringComparison.Ordinal);
        }


        private byte[] GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return saltBytes;
        }

        public IActionResult EditProfile(int userId)
        {
            Console.WriteLine($"EditProfile GET called with userId: {userId}");
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                Console.WriteLine($"User with userId: {userId} not found.");
                return NotFound();
            }

            EditProfileViewModel model = null;

            switch (user.Role?.ToLower()?.Trim())
            {
                case "admin":
                    var admin = _context.Administrators.FirstOrDefault(a => a.UserId == userId);
                    if (admin != null)
                    {
                        model = new EditProfileViewModel
                        {
                            UserId = user.UserId,
                            Name = admin.Name,
                            Surname = admin.Surname,
                            Role = "admin"
                        };
                        Console.WriteLine($"Admin found: {admin.Name} {admin.Surname}");

                    }
                    break;
                case "student":
                    var student = _context.Students.FirstOrDefault(s => s.UserId == userId);
                    if (student != null)
                    {
                        model = new EditProfileViewModel
                        {
                            UserId = user.UserId,
                            Name = student.Name,
                            Surname = student.Surname,
                            Role = "student"
                        };
                        Console.WriteLine($"Student found: {student.Name} {student.Surname}");

                    }
                    break;
                case "teacher":
                    var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == userId);
                    if (teacher != null)
                    {
                        model = new EditProfileViewModel
                        {
                            UserId = user.UserId,
                            Name = teacher.Name,
                            Surname = teacher.Surname,
                            Telephone = teacher.PhoneNumber,
                            Role = "teacher"
                        };
                        Console.WriteLine($"Teacher found: {teacher.Name} {teacher.Surname}");

                    }
                    break;
            }

            
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            Console.WriteLine($"EditProfile POST called with userId: {model.UserId}");
            Console.WriteLine($"Telephone value: {model.Telephone}");
            model.Telephone = model.Telephone?.Trim();

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            if (ModelState.IsValid)
            {
                switch (model.Role?.ToLower()?.Trim())
                {
                    case "admin":
                        var admin = _context.Administrators.FirstOrDefault(a => a.UserId == model.UserId);
                        if (admin != null)
                        {
                            admin.Name = model.Name;
                            admin.Surname = model.Surname;
                            Console.WriteLine($"Admin updated: {admin.Name} {admin.Surname}");

                        }
                        break;
                    case "student":
                        var student = _context.Students.FirstOrDefault(s => s.UserId == model.UserId);
                        if (student != null)
                        {
                            student.Name = model.Name;
                            student.Surname = model.Surname;
                            Console.WriteLine($"Student updated: {student.Name} {student.Surname}");

                        }
                        break;
                    case "teacher":
                        var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == model.UserId);
                        if (teacher != null)
                        {
                            teacher.Name = model.Name;
                            teacher.Surname = model.Surname;
                            if (!string.IsNullOrEmpty(model.Telephone))
                            {
                                if (model.Telephone.Length == 10) // Assuming 10-digit phone number validation
                                {
                                    teacher.PhoneNumber = model.Telephone; // Store as string in database
                                    Console.WriteLine($"Teacher updated: {teacher.Name} {teacher.Surname} with phone: {teacher.PhoneNumber}");
                                }
                                else
                                {
                                    ModelState.AddModelError("Telephone", "Please enter a valid 10-digit telephone number");
                                    return View(model);
                                }
                            }
                        }
                        break;
                }
            

                _context.SaveChanges();
                Console.WriteLine($"Changes saved to the database for userId: {model.UserId}");

                //log in after editing/adding for the 1st time personal info
                var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
                if (user != null)
                {
                    HttpContext.Session.SetString("UserID", user.UserId.ToString());
                    HttpContext.Session.SetString("UserName", user.Username);
                    Console.WriteLine($"Session updated for userId: {user.UserId}");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity)).Wait();
                    Console.WriteLine($"Claims updated and user re-authenticated for userId: {user.UserId}");
                }
                                


                return RedirectToAction("Index", "Home");
            }
            Console.WriteLine("Model state is invalid.");

            return View(model);
        }

        public async Task<IActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            //session variables clear
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home"); 
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Username == id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == "Teacher")
            {
                //teacher information from teacher table
                var teacherInfo = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.UserId);
                ViewBag.TeacherInfo = teacherInfo;
            }
            else if (user.Role == "Student")
            {
                //student information from student table
                var studentInfo = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                ViewBag.StudentInfo = studentInfo;
            }
            else if (user.Role == "Admin")
            {
                //admin information from administrator table
                var adminInfo = await _context.Administrators.FirstOrDefaultAsync(a => a.UserId == user.UserId);
                ViewBag.AdministratorInfo = adminInfo;
            }

            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Username == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string username)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            // role of the user => delete records based on the role
            if (user.Role == "Student")
            {
                //if user is student, delete record from student table
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                if (student != null)
                {
                    // Find all course enrollments of the student
                    var courseEnrollments = await _context.CourseHasStudents.Where(cs => cs.StudentId == student.UserId).ToListAsync();

                    // Remove the student from each course
                    foreach (var enrollment in courseEnrollments)
                    {
                        _context.CourseHasStudents.Remove(enrollment);
                    }
                    _context.Students.Remove(student);
                }
            }
            else if (user.Role == "Teacher")
            {
                //if user is teacher, delete record from teacher table
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.UserId);
                if (teacher != null)
                {
                    // Update the association with courses to null
                    foreach (var course in teacher.Courses)
                    {
                        course.TeacherId = 0; //to be modified later on 
                    }
                    _context.Teachers.Remove(teacher);
                }
            }
            else if (user.Role == "Admin")
            {
                //if user is admin, delete record from administrator table
                var admin = await _context.Administrators.FirstOrDefaultAsync(a => a.UserId == user.UserId);
                if (admin != null)
                {
                    _context.Administrators.Remove(admin);
                }
            }

            // Delete user 
            _context.Users.Remove(user);

            // Handle related records (if necessary)

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


    }


}
