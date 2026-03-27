using SmartBorrowLK.Models;
using System.Collections.Generic;

namespace SmartBorrowLK.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalListings { get; set; }
        public int PendingListingsCount { get; set; }
        public int ApprovedListingsCount { get; set; }
        public int RejectedListingsCount { get; set; }
        public int HighRiskCount { get; set; } // Items with a score > 70
        
        public List<Listing> PendingListings { get; set; } = new();
    }
}