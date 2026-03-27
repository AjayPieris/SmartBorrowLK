using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            // Auto-login after registration
            SetUserSession(user.Id, user.Role, user.Name);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.LoginAsync(model);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            SetUserSession(user.Id, user.Role, user.Name);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private void SetUserSession(int id, string role, string name)
        {
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserName", name);
        }
    }
}