using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(AppDbContext context, IAIService aiService, ILogger<ReviewService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Review?> AddReviewAsync(int userId, CreateReviewViewModel model)
        {
            string cleanComment = model.Comment;
            int trustScore = 50; // default

            try
            {
                _logger.LogInformation("Analyzing review with Gemini AI...");
                var aiResponse = await _aiService.AnalyzeReviewAsync(model.Comment);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    // Extract JSON block between first { and last }
                    int startIdx = aiResponse.IndexOf('{');
                    int endIdx = aiResponse.LastIndexOf('}');

                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        var cleanJson = aiResponse.Substring(startIdx, endIdx - startIdx + 1);
                        _logger.LogInformation("Review AI JSON: {Json}", cleanJson);

                        var aiData = JsonSerializer.Deserialize<AIReviewData>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (aiData != null)
                        {
                            cleanComment = aiData.CleanComment;
                            trustScore = aiData.TrustScore;
                            _logger.LogInformation("Review AI: Sentiment={Sentiment}, TrustScore={Score}", 
                                aiData.Sentiment, aiData.TrustScore);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No JSON found in AI review response: {Response}", aiResponse);
                    }
                }
                else
                {
                    _logger.LogWarning("AI review analysis returned empty. Using raw comment.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze review with AI. Using raw comment.");
            }

            var review = new Review
            {
                ListingId = model.ListingId,
                UserId = userId,
                Rating = model.Rating,
                Comment = cleanComment,
                TrustScore = trustScore
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Review saved: ListingId={ListingId}, Rating={Rating}, TrustScore={Score}", 
                model.ListingId, model.Rating, trustScore);
            
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