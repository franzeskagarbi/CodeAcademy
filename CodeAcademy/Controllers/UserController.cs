using CodeAcademy.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CodeAcademy.Controllers
{
    public class UserController : Controller
    {
        private readonly AcademyContext _context;
        private readonly ISession _session;
        public IActionResult Index()
        {
            return View();
        }

        public UserController(AcademyContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _session = httpContextAccessor.HttpContext.Session;
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
                        return RedirectToAction("UserDashboard");
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

        public IActionResult UserDashBoard()
        {
            if (User.Identity.IsAuthenticated)
            {
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
                    // Set session parameters
                    _session.SetString("UserID", user.UserId.ToString());
                    _session.SetString("UserName", user.Username);
                    return RedirectToAction(nameof(UserDashBoard));
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
            var user = _context.Users.Find(userId);
            if (user == null)
            {
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
                            Role = "teacher"
                        };
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
                        }
                        break;
                    case "student":
                        var student = _context.Students.FirstOrDefault(s => s.UserId == model.UserId);
                        if (student != null)
                        {
                            student.Name = model.Name;
                            student.Surname = model.Surname;
                        }
                        break;
                    case "teacher":
                        var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == model.UserId);
                        if (teacher != null)
                        {
                            teacher.Name = model.Name;
                            teacher.Surname = model.Surname;
                        }
                        break;
                }

                _context.SaveChanges();
                //log in after editing/adding for the 1st time personal info
                var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
                if (user != null)
                {
                    HttpContext.Session.SetString("UserID", user.UserId.ToString());
                    HttpContext.Session.SetString("UserName", user.Username);
                }

                return RedirectToAction("UserDashboard");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            //session variables clear
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home"); 
        }


    }


}
