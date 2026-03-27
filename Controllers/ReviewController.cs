using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IListingService _listingService;

        public ReviewController(IReviewService reviewService, IListingService listingService)
        {
            _reviewService = reviewService;
            _listingService = listingService;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        [HttpGet]
        public async Task<IActionResult> Create(int listingId)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Auth");

            var listing = await _listingService.GetListingByIdAsync(listingId);
            if (listing == null) return NotFound();

            var model = new CreateReviewViewModel
            {
                ListingId = listing.Id,
                ItemName = listing.Item.Name
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var review = await _reviewService.AddReviewAsync(userId.Value, model);
            
            if (review == null)
            {
                ModelState.AddModelError("", "Failed to process review. Please try again.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Thank you! Your review has been analyzed and published.";
            return RedirectToAction("MyBookings", "Booking"); // Send them back to their booking history
        }
    }
}