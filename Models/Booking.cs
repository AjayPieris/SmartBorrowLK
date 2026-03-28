using System;

namespace SmartBorrowLK.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    }
}