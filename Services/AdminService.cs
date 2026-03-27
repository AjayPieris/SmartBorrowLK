using Microsoft.EntityFrameworkCore;
using SmartBorrowLK.Data;
using SmartBorrowLK.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBorrowLK.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardSummaryAsync();
        Task<bool> ApproveListingAsync(int id);
        Task<bool> RejectListingAsync(int id);
    }

    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardSummaryAsync()
        {
            var allListings = await _context.Listings
                .Include(l => l.Item)
                .Include(l => l.Owner)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                TotalListings = allListings.Count,
                PendingListingsCount = allListings.Count(l => l.Status == "Pending"),
                ApprovedListingsCount = allListings.Count(l => l.Status == "Approved"),
                RejectedListingsCount = allListings.Count(l => l.Status == "Rejected"),
                HighRiskCount = allListings.Count(l => l.RiskScore >= 70),
                
                PendingListings = allListings
                    .Where(l => l.Status == "Pending")
                    .OrderByDescending(l => l.RiskScore) // Show highest risk first
                    .ThenBy(l => l.CreatedAt)
                    .ToList()
            };
        }

        public async Task<bool> ApproveListingAsync(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null || listing.Status != "Pending") return false;

            listing.Status = "Approved";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectListingAsync(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null || listing.Status != "Pending") return false;

            listing.Status = "Rejected";
            await _context.SaveChangesAsync();
            return true;
        }
    }
}