using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

#pragma warning disable
namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class MunosabatAiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string GeminiApiKey = "AIzaSyBQmWF2kylKyYLt5SC5fsS47C1G4MwDBt8";

    private const string SystemPrompt =
        "Siz munosabatlar bo'yicha mutaxassis maslahatchi AI siz. " +
        "Asosan er-xotin o'rtasidagi nizolar, sevgi va munosabatlardagi muammolarga maslahat berasiz. " +
        "Har doim hamdard, sabr-toqatli va muloyim ohangda javob bering. " +
        "Ikki tomonni ham tushunishga harakat qiling, bir tomonni ayblamang. " +
        "Amaliy va hayotiy maslahatlar bering. " +
        "Faqat munosabatlar, oila, sevgi va kommunikatsiya mavzularida javob bering. " +
        "Agar boshqa mavzuda so'ralsa, muloyimlik bilan rad eting va munosabatlar haqida gaplashishni taklif qiling. " +
        "Qaysi tilda so'ralsa, o'sha tilda javob bering. " +
        "Javobingizni qisqa va aniq qiling, lekin to'liq maslahat bering.";

    public MunosabatAiController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("maslahat")]
    public async Task<IActionResult> Maslahat([FromBody] MunosabatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Muammo))
            {
                return StatusCode(400, new
                {
                    code = 400,
                    error = "Muammo maydoni bo'sh bo'lishi mumkin emas.",
                    javob = (string)null
                });
            }

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={GeminiApiKey}";

            var fullPrompt = $"{SystemPrompt}\n\nFoydalanuvchi muammosi: {request.Muammo}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
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
                    error = $"Gemini API xatosi: {result}",
                    javob = (string)null
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
                    javob = "Kechirasiz, javob olishda muammo yuz berdi. Iltimos qayta urinib ko'ring."
                });
            }

            aiText = aiText.Trim();

            return StatusCode(200, new
            {
                code = 200,
                error = (string)null,
                javob = aiText
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                code = 500,
                error = $"Server xatosi: {ex.Message}",
                javob = (string)null
            });
        }
    }
}

public class MunosabatRequest
{
    public string Muammo { get; set; }
}
