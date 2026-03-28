using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ListingService> _logger;

        public ListingService(AppDbContext context, ICloudinaryService cloudinary, IAIService aiService, ILogger<ListingService> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Listing?> CreateListingAsync(int userId, CreateListingViewModel model)
        {
            // 1. Read the image file into a byte array FIRST (before any service consumes the stream)
            byte[]? imageBytes = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.Image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            // 2. Upload Image to Cloudinary using the byte array (safe - no double stream read)
            string imageUrl = string.Empty;
            if (imageBytes != null)
            {
                imageUrl = await _cloudinary.UploadImageAsync(imageBytes, model.Image!.FileName);
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogError("Cloudinary upload failed for user {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Image uploaded to Cloudinary: {Url}", imageUrl);

            // 3. Convert image bytes to base64 for Gemini AI analysis
            string? base64Image = imageBytes != null ? Convert.ToBase64String(imageBytes) : null;

            // 4. Call Gemini AI to generate listing details
            string aiDescription = model.RawDescription ?? "";
            decimal pricePerDay = model.ManualPrice ?? 0;
            string terms = "Standard rental terms apply. Item must be returned in the same condition.";

            _logger.LogInformation("Calling Gemini AI for listing generation...");
            var aiResponse = await _aiService.GenerateListingDetailsAsync(model.RawDescription, base64Image);
            _logger.LogInformation("Gemini AI response length: {Length}", aiResponse?.Length ?? 0);

            if (!string.IsNullOrEmpty(aiResponse))
            {
                try
                {
                    // Extract JSON block between the first { and last }
                    int startIdx = aiResponse.IndexOf('{');
                    int endIdx = aiResponse.LastIndexOf('}');

                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        var cleanJson = aiResponse.Substring(startIdx, endIdx - startIdx + 1);
                        _logger.LogInformation("Extracted JSON: {Json}", cleanJson);

                        var aiData = JsonSerializer.Deserialize<AIGeneratedData>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (aiData != null)
                        {
                            _logger.LogInformation("AI generated: Desc={Desc}, Price={Price}, Terms={Terms}",
                                aiData.Description?.Length > 50 ? aiData.Description[..50] + "..." : aiData.Description,
                                aiData.PricePerDay,
                                aiData.Terms?.Length > 50 ? aiData.Terms[..50] + "..." : aiData.Terms);

                            aiDescription = aiData.Description;
                            terms = aiData.Terms;
                            // Use manual price if provided, otherwise use AI price
                            if (!model.ManualPrice.HasValue || model.ManualPrice.Value <= 0)
                            {
                                pricePerDay = aiData.PricePerDay;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("AI response deserialized to null");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No JSON object found in AI response: {Response}", aiResponse);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse AI response JSON: {Response}", aiResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing AI response");
                }
            }
            else
            {
                _logger.LogWarning("Gemini AI returned empty response. Using manual/default values.");
            }

            // Ensure we have a valid price
            if (pricePerDay <= 0)
            {
                pricePerDay = model.ManualPrice ?? 500; // Default fallback
                _logger.LogWarning("Price was 0 or negative. Using fallback: {Price}", pricePerDay);
            }

            // 5. Get AI risk score (with fallback)
            int riskScore = 25; // default
            try
            {
                _logger.LogInformation("Calculating AI risk score...");
                riskScore = await _aiService.CalculateRiskScoreAsync(aiDescription, pricePerDay);
                _logger.LogInformation("AI Risk Score: {Score}", riskScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Risk score calculation failed. Using default: 25");
                riskScore = 25;
            }

            // 6. Save to Database (PostgreSQL via Neon)
            var item = new Item
            {
                Name = model.ItemName,
                Condition = model.Condition,
                Description = model.RawDescription,
                ImageUrl = imageUrl
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            var listing = new Listing
            {
                ItemId = item.Id,
                OwnerId = userId,
                PricePerDay = pricePerDay,
                Description = aiDescription,
                Terms = terms,
                RiskScore = riskScore,
                Status = riskScore > 80 ? "Rejected" : "Pending"
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Listing created successfully: ID={Id}, Price={Price}, Risk={Risk}, Status={Status}",
                listing.Id, listing.PricePerDay, listing.RiskScore, listing.Status);

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
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
    }
}