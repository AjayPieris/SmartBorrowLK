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

            // 2. Try to call Gemini AI to generate details
            string aiDescription = model.RawDescription;
            decimal pricePerDay = model.ManualPrice ?? 0;
            string terms = "Standard rental terms apply. Item must be returned in the same condition.";
            
            var aiResponse = await _aiService.GenerateListingDetailsAsync(model.RawDescription);
            
            if (!string.IsNullOrEmpty(aiResponse))
            {
                try
                {
                    // Clean markdown formatting if Gemini includes it (e.g., ```json ... ```)
                    var cleanJson = aiResponse.Replace("```json", "").Replace("```", "").Trim();
                    var aiData = JsonSerializer.Deserialize<AIGeneratedData>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (aiData != null)
                    {
                        aiDescription = aiData.Description;
                        terms = aiData.Terms;
                        // Use manual price if provided, otherwise use AI price
                        if (!model.ManualPrice.HasValue || model.ManualPrice.Value <= 0)
                        {
                            pricePerDay = aiData.PricePerDay;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse AI response: {ex.Message}");
                    // Continue with manual/default values
                }
            }

            // Ensure we have a valid price
            if (pricePerDay <= 0)
            {
                pricePerDay = model.ManualPrice ?? 500; // Default fallback
            }

            // 3. Try to get AI risk score (with fallback)
            int riskScore = 0;
            try
            {
                riskScore = await _aiService.CalculateRiskScoreAsync(aiDescription, pricePerDay);
            }
            catch
            {
                riskScore = 25; // Default low-risk score if AI fails
            }

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
                PricePerDay = pricePerDay,
                Description = aiDescription,
                Terms = terms,
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