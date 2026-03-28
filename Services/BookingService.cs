using Microsoft.EntityFrameworkCore;
using SmartBorrowLK.Data;
using SmartBorrowLK.Models;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Services
{
    public interface IBookingService
    {
        Task<bool> IsItemAvailableAsync(int listingId, DateTime startDate, DateTime endDate);
        Task<Booking?> CreateBookingAsync(int userId, CreateBookingViewModel model);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<List<Booking>> GetVendorBookingsAsync(int vendorId);
        Task<bool> UpdateBookingStatusAsync(int bookingId, int vendorId, string status);
    }

    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsItemAvailableAsync(int listingId, DateTime startDate, DateTime endDate)
        {
            // Ensure dates are UTC for Postgres
            var utcStartDate = startDate.Kind == DateTimeKind.Utc ? startDate : startDate.ToUniversalTime();
            var utcEndDate = endDate.Kind == DateTimeKind.Utc ? endDate : endDate.ToUniversalTime();

            // Check if there are any existing bookings for this listing that overlap with the requested dates
            bool hasOverlap = await _context.Bookings.AnyAsync(b =>
                b.ListingId == listingId &&
                b.Status != "Rejected" &&
                b.StartDate < utcEndDate &&
                b.EndDate > utcStartDate);

            return !hasOverlap;
        }

        public async Task<Booking?> CreateBookingAsync(int userId, CreateBookingViewModel model)
        {
            var isAvailable = await IsItemAvailableAsync(model.ListingId, model.StartDate, model.EndDate);
            if (!isAvailable) return null;

            var booking = new Booking
            {
                ListingId = model.ListingId,
                UserId = userId,
                StartDate = model.StartDate.ToUniversalTime(), // Postgres requires UTC dates
                EndDate = model.EndDate.ToUniversalTime()
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Item)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.StartDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetVendorBookingsAsync(int vendorId)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Item)
                .Include(b => b.User)
                .Where(b => b.Listing.OwnerId == vendorId)
                .OrderByDescending(b => b.StartDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, int vendorId, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Listing.OwnerId == vendorId);

            if (booking == null) return false;

            booking.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}