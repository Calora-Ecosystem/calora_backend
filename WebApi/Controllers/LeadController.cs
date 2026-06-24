using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using Serilog;

namespace WebApi.Controllers;

/// <summary>
/// Receives leads from the landing page and forwards them to a Telegram group.
/// Nothing is stored on the backend — the controller is a pure pass-through to
/// the Telegram bot. Everything (DTO + bot logic) is intentionally kept inside
/// this single file.
/// </summary>
[ApiController]
[Route("leads")]
[AllowAnonymous]
public class LeadController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    // Telegram bot that posts incoming leads into the target group chat.
    private const string BotToken = "8704930334:AAHN8DvqsNbRqEhQRMxM2c5I4Y-dJ6Mb3Ao";
    private const string ChatId = "-1003973937260";

    [HttpPost]
    public async Task<Wrapper> Create([FromBody] LeadDto dto)
    {
        var text = BuildMessage(dto);
        await SendToTelegramAsync(text, HttpContext.RequestAborted);
        return (true, 200);
    }

    private static string BuildMessage(LeadDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🟢 <b>Yangi lead — Whiteline</b>");
        sb.AppendLine();
        sb.AppendLine($"👤 <b>Ism:</b> {Escape(dto.Name)}");
        sb.AppendLine($"📞 <b>Tel / Telegram:</b> {Escape(dto.Phone)}");

        if (dto.Services is { Count: > 0 })
            sb.AppendLine($"🧩 <b>Xizmat:</b> {Escape(string.Join(", ", dto.Services))}");

        if (!string.IsNullOrWhiteSpace(dto.Note))
            sb.AppendLine($"📝 <b>Izoh:</b> {Escape(dto.Note!)}");

        if (!string.IsNullOrWhiteSpace(dto.Source))
            sb.AppendLine($"🌐 <b>Manba:</b> {Escape(dto.Source!)}");

        return sb.ToString();
    }

    private async Task SendToTelegramAsync(string text, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"https://api.telegram.org/bot{BotToken}/sendMessage",
            new
            {
                chat_id = ChatId,
                text,
                parse_mode = "HTML",
                disable_web_page_preview = true
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Error("Telegram sendMessage failed ({Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Telegram sendMessage failed: {response.StatusCode}");
        }
    }

    // Telegram HTML parse_mode requires these characters to be escaped.
    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public class LeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Phone { get; set; } = null!;

        public List<string>? Services { get; set; }

        [MaxLength(2000)]
        public string? Note { get; set; }

        [MaxLength(200)]
        public string? Source { get; set; }
    }
}
