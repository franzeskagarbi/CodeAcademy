using CodeAcademy.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpPost]
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
        }

        public IActionResult UserDashBoard()
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

                // Add the user to the context
                _context.Add(user);

                // Save changes to the database
                try
                {
                    await _context.SaveChangesAsync();
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
    }

    
}
