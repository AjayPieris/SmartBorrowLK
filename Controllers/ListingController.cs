using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Controllers
{
    public class ListingController : Controller
    {
        private readonly IListingService _listingService;

        public ListingController(IListingService listingService)
        {
            _listingService = listingService;
        }

        // Helper to check if user is logged in
        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        public async Task<IActionResult> Index()
        {
            var listings = await _listingService.GetApprovedListingsAsync();
            return View(listings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var listing = await _listingService.GetListingByIdAsync(id);
            if (listing == null) return NotFound();
            return View(listing);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateListingViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var listing = await _listingService.CreateListingAsync(userId.Value, model);
            
            if (listing == null)
            {
                ModelState.AddModelError("", "Failed to generate listing. Please try again.");
                return View(model);
            }

            return RedirectToAction("MyListings");
        }

        public async Task<IActionResult> MyListings()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var listings = await _listingService.GetUserListingsAsync(userId.Value);
            return View(listings);
        }
    }
}