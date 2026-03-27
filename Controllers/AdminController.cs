using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using System.Threading.Tasks;

namespace SmartBorrowLK.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // Simple custom security check
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home"); // Kick non-admins out

            var model = await _adminService.GetDashboardSummaryAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            await _adminService.ApproveListingAsync(id);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            await _adminService.RejectListingAsync(id);
            return RedirectToAction("Dashboard");
        }
    }
}