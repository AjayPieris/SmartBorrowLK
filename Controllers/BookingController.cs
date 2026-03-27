using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IListingService _listingService;

        public BookingController(IBookingService bookingService, IListingService listingService)
        {
            _bookingService = bookingService;
            _listingService = listingService;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        [HttpGet]
        public async Task<IActionResult> Book(int listingId)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Auth");

            var listing = await _listingService.GetListingByIdAsync(listingId);
            if (listing == null || listing.Status != "Approved") return NotFound();

            var model = new CreateBookingViewModel
            {
                ListingId = listing.Id,
                ItemName = listing.Item.Name,
                PricePerDay = listing.PricePerDay
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Book(CreateBookingViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var booking = await _bookingService.CreateBookingAsync(userId.Value, model);
            
            if (booking == null)
            {
                ModelState.AddModelError("", "Sorry, this item is already booked for the selected dates.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Booking confirmed successfully!";
            return RedirectToAction("MyBookings");
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var bookings = await _bookingService.GetUserBookingsAsync(userId.Value);
            return View(bookings);
        }
    }
}