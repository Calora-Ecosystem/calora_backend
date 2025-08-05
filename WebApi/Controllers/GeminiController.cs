using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

#pragma warning disable
namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class GeminiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GeminiController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskGemini([FromBody] GeminiRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HashCode) || string.IsNullOrWhiteSpace(request.Question))
                {
                    return StatusCode(400, new
                    {
                        code = 400,
                        error = "HashCode and Prompt are required fields.",
                        content = (string)null
                    });
                }

                if (request.HashCode != GeminiSecret.ServerHashCode)
                {
                    return StatusCode(401, new
                    {
                        code = 401,
                        error = "Invalid hash code provided.",
                        content = (string)null
                    });
                }

                var apiKey = GeminiSecret.GeminiApiKey;
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

                string newPrompt = $"{request.Question}\n\nFikr: Siz faqat Andeli stabilizatorlari haqida qisqa gapirishingiz kerak. Agar boshqa mavzuda so‘ralgan bo‘lsa, javob bermang. Javob berganda esa qisqa javob bering va qaysi tilda so‘ralsa shu tilda javob bering. Mijozga ko‘proq Andeli mahsulotlarini taklif qiling. Qisqa javob yozib ber va taklifingni yaxshilab o‘ylab keyin taklif qil!";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = newPrompt }
                            }
                        }
                    }
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        code = (int)response.StatusCode,
                        error = $"Request failed with status code {response.StatusCode}: {result}",
                        content = (string)null
                    });
                }

                dynamic parsed = JsonConvert.DeserializeObject(result);
                string aiText = parsed?.candidates?[0]?.content?.parts?[0]?.text?.ToString();

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    return StatusCode(200, new
                    {
                        code = 200,
                        error = (string)null,
                        content = ""
                    });
                }

                aiText = Regex.Replace(aiText, @"\\n|\s+", " ");
                aiText = Regex.Replace(aiText, @"\*+", "").Replace("\\\"", "\"").Trim();

                return StatusCode(200, new
                {
                    code = 200,
                    error = (string)null,
                    content = aiText
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 500,
                    error = $"Internal server error: {ex.Message}",
                    content = (string)null
                });
            }
        }

        public class GeminiRequest
        {
            public string HashCode { get; set; }
            public string Question { get; set; }
        }

        public static class GeminiSecret
        {
            public static readonly string GeminiApiKey = "AIzaSyA49FqJbBAYhBI8mXpeXT3R95tYezuzBrU";
            public static readonly string ServerHashCode = "$2y$10$EylyeBAtyJCPoN8TMzmqvuwd.cs6LGYubIeZHfthjzA6XMQUttJHG";
        }
    }
}
