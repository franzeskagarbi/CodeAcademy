using CodeAcademy.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options =>
               {
                   options.LoginPath = "/Account/Login";
                   options.LogoutPath = "/Account/Logout";
               });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);//set the session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// My DI for EF
builder.Services.AddDbContext<AcademyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAuthentication();

//session call after routing

app.UseSession();
app.UseMiddleware<ClearSession>();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
   name: "enroll",
            pattern: "Courses/Enroll/{id}",
            defaults: new { controller = "Courses", action = "EnrollGet" });

app.MapControllerRoute(
    name: "editTeacherProfile",
    pattern: "/user/editprofile/teacher/{userId}",
    defaults: new { controller = "User", action = "EditTeacherProfile" });

app.MapControllerRoute(
    name: "editProfile",
    pattern: "/user/editprofile/adminstudent",
    defaults: new { controller = "User", action = "EditProfile" });




app.Run();
