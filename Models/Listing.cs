using System;
using System.Collections.Generic;

namespace SmartBorrowLK.Models
{
    public class Listing
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public decimal PricePerDay { get; set; }
        public string Description { get; set; } = string.Empty; // AI Generated
        public string Terms { get; set; } = string.Empty; // AI Generated
        public int RiskScore { get; set; } // AI Generated (0-100)
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}