using Microsoft.AspNetCore.Mvc;
using SmartBorrowLK.Services;
using SmartBorrowLK.ViewModels;
using System.Security.Claims;

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

        private int? GetCurrentUserId() => User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0") : null;

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

        public async Task<IActionResult> Requests()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            // For vendors only
            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value != "Vendor")
            {
                return Unauthorized();
            }

            var bookings = await _bookingService.GetVendorBookingsAsync(userId.Value);
            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            bool success = await _bookingService.UpdateBookingStatusAsync(id, userId.Value, status);

            if (success)
            {
                TempData["SuccessMessage"] = $"Booking {status.ToLower()} successfully.";
            }

            return RedirectToAction("Requests");
        }
    }
}