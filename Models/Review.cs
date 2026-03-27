namespace SmartBorrowLK.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public int TrustScore { get; set; } // AI Generated
    }
}