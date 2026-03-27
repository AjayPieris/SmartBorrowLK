using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartBorrowLK.Services
{
    public interface IAIService
    {
        Task<string> GenerateListingDetailsAsync(string inputText);
        Task<int> CalculateRiskScoreAsync(string description, decimal price);

        Task<string> AnalyzeReviewAsync(string reviewComment);
    }

    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAIService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini ApiKey is missing");
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseString);

                return document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString() ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Gemini API error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string> GenerateListingDetailsAsync(string inputText)
        {
            var prompt = $@"
            You are an AI assistant for a Sri Lankan tech rental marketplace.
            Based on this user input: '{inputText}'
            Generate a JSON response strictly with these keys: 
            'Description' (a professional listing description), 
            'PricePerDay' (suggested realistic price in LKR as a number), 
            'Terms' (standard rental terms protecting the owner).";

            return await CallGeminiApiAsync(prompt);
        }

        public async Task<int> CalculateRiskScoreAsync(string description, decimal price)
        {
            var prompt = $@"
            Analyze this tech rental listing for fraud risk. 
            Description: '{description}', Price: {price} LKR.
            Return ONLY a single integer from 0 to 100, where 100 is extremely high risk (e.g., suspiciously low price for high-end gear, vague descriptions).";

            var response = await CallGeminiApiAsync(prompt);
            int.TryParse(response.Trim(), out int score);
            return score;
        }

        public async Task<string> AnalyzeReviewAsync(string reviewComment)
        {
            var prompt = $@"
    Analyze this rental marketplace review: '{reviewComment}'.
    1. Clean the review (fix minor typos, remove offensive language).
    2. Determine the sentiment (Positive, Neutral, Negative).
    3. Calculate a Trust Score from 0 to 100 based on authenticity and detail.
    
    Return a JSON response strictly with these keys: 
    'CleanComment' (string), 
    'Sentiment' (string), 
    'TrustScore' (integer).";

            return await CallGeminiApiAsync(prompt);
        }
    }
}