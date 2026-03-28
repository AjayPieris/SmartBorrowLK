using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartBorrowLK.Services
{
    public interface IAIService
    {
        Task<string> GenerateListingDetailsAsync(string? inputText, string? base64Image = null);
        Task<int> CalculateRiskScoreAsync(string description, decimal price);
        Task<string> AnalyzeReviewAsync(string reviewComment);
    }

    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiAIService> _logger;
        private const int MaxRetries = 3;

        public GeminiAIService(HttpClient httpClient, IConfiguration config, ILogger<GeminiAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["Gemini:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Gemini API key is missing! Check your .env file for 'Gemini__ApiKey'");
            }
        }

        private async Task<string> CallGeminiApiAsync(string prompt, string? base64Image = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Gemini API key not configured. Skipping AI call.");
                return string.Empty;
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            object[] parts;
            if (!string.IsNullOrEmpty(base64Image))
            {
                parts = new object[]
                {
                    new { text = prompt },
                    new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                };
            }
            else
            {
                parts = new object[] { new { text = prompt } };
            }

            var requestBody = new
            {
                contents = new[] { new { parts = parts } }
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Gemini API call attempt {Attempt}/{Max}", attempt + 1, MaxRetries);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var waitSeconds = (int)Math.Pow(2, attempt + 1) * 5; // 10s, 20s, 40s
                        _logger.LogWarning("Gemini API rate-limited (429). Waiting {Seconds}s before retry...", waitSeconds);
                        await Task.Delay(waitSeconds * 1000);
                        continue;
                    }

                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Gemini API returned {StatusCode}: {Body}", response.StatusCode, responseString);
                        return string.Empty;
                    }

                    _logger.LogInformation("Gemini API responded successfully.");

                    using var document = JsonDocument.Parse(responseString);
                    var text = document.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString() ?? string.Empty;

                    _logger.LogInformation("Gemini returned: {Response}", text.Length > 200 ? text[..200] + "..." : text);
                    return text;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Gemini API network error on attempt {Attempt}", attempt + 1);
                    if (attempt < MaxRetries - 1)
                    {
                        await Task.Delay((int)Math.Pow(2, attempt) * 2000);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Gemini API response JSON");
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error calling Gemini API");
                    return string.Empty;
                }
            }

            _logger.LogError("Gemini API failed after {MaxRetries} retries", MaxRetries);
            return string.Empty;
        }

        public async Task<string> GenerateListingDetailsAsync(string? inputText, string? base64Image = null)
        {
            var prompt = @"You are an AI assistant for a Sri Lankan tech rental marketplace called SmartBorrowLK.
Based on the user's description and/or the provided image of the item, generate a JSON response.

IMPORTANT: Return ONLY valid JSON with NO markdown formatting, no ```json blocks, no extra text.

The JSON must have exactly these keys:
{
  ""Description"": ""A professional, detailed listing description (2-3 sentences)"",
  ""PricePerDay"": 1500,
  ""Terms"": ""Standard rental terms protecting the owner (1-2 sentences)""
}

- PricePerDay must be a realistic number in Sri Lankan Rupees (LKR). For example: cameras 2000-8000, laptops 3000-10000, drones 4000-12000, gaming consoles 1500-5000.
- If no image or description is given, use reasonable defaults.

User's description: """ + (inputText ?? "No description provided") + @"""";

            return await CallGeminiApiAsync(prompt, base64Image);
        }

        public async Task<int> CalculateRiskScoreAsync(string description, decimal price)
        {
            var prompt = $@"Analyze this tech rental listing for fraud risk.
Description: '{description}', Price: {price} LKR.

IMPORTANT: Return ONLY valid JSON with NO markdown formatting, no ```json blocks, no extra text.

The JSON must have exactly this key:
{{
  ""RiskScore"": 25
}}

0 = no risk, 100 = extremely high risk.
Consider: suspiciously low prices for high-end gear, vague/copy-pasted descriptions, unrealistic claims.";

            var response = await CallGeminiApiAsync(prompt);
            
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Risk score AI returned empty. Using default score of 25.");
                return 25;
            }

            try 
            {
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("RiskScore", out var scoreElement) && scoreElement.TryGetInt32(out int score))
                {
                    return Math.Clamp(score, 0, 100);
                }
            } 
            catch (JsonException) 
            {
                // Fallback digit extraction just in case it didn't output JSON
                var digitsOnly = new string(response.Where(char.IsDigit).ToArray());
                // If the digits length is small (1-3 chars), it's probably just the number 
                if (digitsOnly.Length > 0 && digitsOnly.Length <= 3 && int.TryParse(digitsOnly, out int score))
                {
                    return Math.Clamp(score, 0, 100);
                }
            }

            _logger.LogWarning("Could not parse risk score from: '{Response}'. Using default 25.", response);
            return 25;
        }

        public async Task<string> AnalyzeReviewAsync(string reviewComment)
        {
            var prompt = $@"Analyze this rental marketplace review: '{reviewComment}'.

IMPORTANT: Return ONLY valid JSON with NO markdown formatting, no ```json blocks, no extra text.

The JSON must have exactly these keys:
{{
  ""CleanComment"": ""The cleaned review text with typos fixed and offensive language removed"",
  ""Sentiment"": ""Positive"",
  ""TrustScore"": 75
}}

- Sentiment must be one of: Positive, Neutral, Negative
- TrustScore must be an integer from 0 to 100";

            return await CallGeminiApiAsync(prompt);
        }
    }
}