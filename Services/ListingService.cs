using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SmartBorrowLK.Data;
using SmartBorrowLK.Models;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Services
{
    public interface IListingService
    {
        Task<Listing?> CreateListingAsync(int userId, CreateListingViewModel model);
        Task<List<Listing>> GetApprovedListingsAsync();
        Task<List<Listing>> GetUserListingsAsync(int userId);
        Task<Listing?> GetListingByIdAsync(int id);
    }

    // Helper class to parse Gemini's JSON response
    public class AIGeneratedData 
    {
        public string Description { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }
        public string Terms { get; set; } = string.Empty;
    }

    public class ListingService : IListingService
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly IAIService _aiService;

        public ListingService(AppDbContext context, ICloudinaryService cloudinary, IAIService aiService)
        {
            _context = context;
            _cloudinary = cloudinary;
            _aiService = aiService;
        }

        public async Task<Listing?> CreateListingAsync(int userId, CreateListingViewModel model)
        {
            // 1. Upload Image to Cloudinary
            var imageUrl = await _cloudinary.UploadImageAsync(model.Image);
            if (string.IsNullOrEmpty(imageUrl)) return null;

            // 2. Call Gemini AI to generate details
            var aiResponse = await _aiService.GenerateListingDetailsAsync(model.RawDescription);
            
            // Clean markdown formatting if Gemini includes it (e.g., ```json ... ```)
            var cleanJson = aiResponse.Replace("```json", "").Replace("```", "").Trim();
            var aiData = JsonSerializer.Deserialize<AIGeneratedData>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (aiData == null) return null;

            // 3. Call Gemini AI for Risk Analysis
            var riskScore = await _aiService.CalculateRiskScoreAsync(aiData.Description, aiData.PricePerDay);

            // 4. Save to Database (PostgreSQL via Neon)
            var item = new Item
            {
                Name = model.ItemName,
                Condition = model.Condition,
                Description = model.RawDescription,
                ImageUrl = imageUrl
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync(); // Save to get the ItemId

            var listing = new Listing
            {
                ItemId = item.Id,
                OwnerId = userId,
                PricePerDay = aiData.PricePerDay,
                Description = aiData.Description,
                Terms = aiData.Terms,
                RiskScore = riskScore,
                // If risk is too high, auto-reject. Otherwise, pending admin approval.
                Status = riskScore > 80 ? "Rejected" : "Pending" 
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return listing;
        }

        public async Task<List<Listing>> GetApprovedListingsAsync()
        {
            return await _context.Listings
                .Include(l => l.Item)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Approved")
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Listing>> GetUserListingsAsync(int userId)
        {
            return await _context.Listings
                .Include(l => l.Item)
                .Where(l => l.OwnerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<Listing?> GetListingByIdAsync(int id)
        {
            return await _context.Listings
                .Include(l => l.Item)
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
    }
}