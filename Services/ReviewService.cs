using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SmartBorrowLK.Data;
using SmartBorrowLK.Models;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Services
{
    public interface IReviewService
    {
        Task<Review?> AddReviewAsync(int userId, CreateReviewViewModel model);
        Task<List<Review>> GetReviewsForListingAsync(int listingId);
    }

    public class AIReviewData
    {
        public string CleanComment { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public int TrustScore { get; set; }
    }

    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;

        public ReviewService(AppDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public async Task<Review?> AddReviewAsync(int userId, CreateReviewViewModel model)
        {
            // Call Gemini to analyze the raw comment
            var aiResponse = await _aiService.AnalyzeReviewAsync(model.Comment);
            
            var cleanJson = aiResponse.Replace("```json", "").Replace("```", "").Trim();
            var aiData = JsonSerializer.Deserialize<AIReviewData>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (aiData == null) return null;

            var review = new Review
            {
                ListingId = model.ListingId,
                UserId = userId,
                Rating = model.Rating,
                Comment = aiData.CleanComment, // Save the AI-cleaned comment
                TrustScore = aiData.TrustScore
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<List<Review>> GetReviewsForListingAsync(int listingId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ListingId == listingId)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }
    }
}