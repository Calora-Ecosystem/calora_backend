using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebCore;

#pragma warning disable
namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
[EnableCors(ApplicationConfigurationExtensions.OpenCorsPolicy)]
public class MunosabatAiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string GeminiApiKey = "AIzaSyBQmWF2kylKyYLt5SC5fsS47C1G4MwDBt8";

    private const string SystemPrompt =
        "Sen munosabatlar bo'yicha samimiy va hamdard do'st-maslahatchi AIsan. " +
        "Do'st yoki ona kabi — issiq, mehribon, hukm qilmaydigan ohangda gaplash. " +
        "Sening birinchi va eng muhim vazifang: MUAMMONI TO'LIQ TUSHUNIB OLISH. " +

        "QOIDA 1 — MUAMMONI ANIQLASH: " +
        "Agar user qisqa yoki umumiy muammo yozsa (masalan 'erim bilan urishyapman', 'sevgilim bilan muammo bor'), " +
        "darhol maslahat berma. Avval muammoning asosini tushunib ol. " +
        "Faqat BITTA aniq savol ber — eng muhim narsani so'ra. " +
        "Masalan: 'Voy, bu og'ir ekan... Aytsang-chi, urish nimadan boshlandi? Qanday gap bo'ldi?' " +
        "yoki 'Tushunaman seni... Asosan nima sababdan urishyapsizlar, biror takrorlanib turgan narsa bormi?' " +

        "QOIDA 2 — YETARLI MA'LUMOT BO'LSA MASLAHAT BER: " +
        "User muammoni batafsil aytgan bo'lsa yoki savolingga javob bergan bo'lsa, " +
        "u holda aniq va amaliy maslahat ber. Ikkala tomonni ham tushun, hech kimni ayblama. " +

        "QOIDA 3 — DO'STONA USLUB: " +
        "Har doim samimiy, issiqqina gaplash. 'Tushunaman', 'Bu og'ir ekan', 'Sen yolg'iz emassan' kabi " +
        "ko'ngilni ko'taradigan so'zlardan foydalangin. Rasmiy yoki quruq bo'lma. " +

        "QOIDA 4 — FAQAT MUNOSABATLAR MAVZUSI: " +
        "Faqat munosabatlar, sevgi, oila, kommunikatsiya haqida javob ber. " +
        "Boshqa mavzularda muloyimlik bilan: 'Men faqat munosabatlar bo'yicha maslahat bera olaman' de. " +

        "QOIDA 5 — TIL: Qaysi tilda yozilsa o'sha tilda javob ber. " +

        "ESLAB QOL: Birinchi javobda asosiy savol — muammoning SABABI nima ekanligini bil. " +
        "Keyin yechim taklif qil. Shoshma, avval tinglа.";

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
