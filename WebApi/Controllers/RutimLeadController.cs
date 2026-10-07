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
/// Receives leads from the RUTIM B2B landing page (Instagram target) and forwards
/// them to the managers' Telegram group. Nothing is stored on the backend — the
/// controller is a pure pass-through to the Telegram bot, same as
/// <see cref="LeadController"/>. Everything (DTO + bot logic) is intentionally
/// kept inside this single file.
/// </summary>
[ApiController]
[Route("leads/rutim")]
[AllowAnonymous]
public class RutimLeadController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    // @rutim_b2b_leadbot — posts incoming leads into the RUTIM managers' group.
    private const string BotToken = "8285599009:AAE0WzTriJHEz-rGj2875W3mLB8yWdeJawo";
    private const string ChatId = "-5126325650";

    [HttpPost]
    public async Task<Wrapper> Create([FromBody] RutimLeadDto dto)
    {
        var text = BuildMessage(dto);
        await SendToTelegramAsync(text, HttpContext.RequestAborted);
        return (true, 200);
    }

    private static string BuildMessage(RutimLeadDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🔌 <b>Yangi ariza — RUTIM</b>");
        sb.AppendLine();
        sb.AppendLine($"👤 <b>Ism:</b> {Escape(dto.Name)}");
        sb.AppendLine($"📞 <b>Telefon:</b> {Escape(dto.Phone)}");

        if (!string.IsNullOrWhiteSpace(dto.Shop))
            sb.AppendLine($"🏪 <b>Do'kon:</b> {Escape(dto.Shop)}");

        if (!string.IsNullOrWhiteSpace(dto.City))
            sb.AppendLine($"📍 <b>Shahar:</b> {Escape(dto.City)}");

        if (!string.IsNullOrWhiteSpace(dto.Type))
            sb.AppendLine($"🤝 <b>Hamkorlik:</b> {Escape(dto.Type)}");

        if (!string.IsNullOrWhiteSpace(dto.Model))
            sb.AppendLine($"📦 <b>Model:</b> {Escape(dto.Model)}");

        if (!string.IsNullOrWhiteSpace(dto.Lang) || !string.IsNullOrWhiteSpace(dto.Page))
        {
            sb.AppendLine();
            sb.AppendLine($"🌐 {Escape(dto.Lang)} · {Escape(Cut(dto.Page, 300))}");
        }

        if (!string.IsNullOrWhiteSpace(dto.Utm))
            sb.AppendLine($"📊 {Escape(Cut(dto.Utm, 300))}");

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
            Log.Error("RUTIM lead: Telegram sendMessage failed ({Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Telegram sendMessage failed: {response.StatusCode}");
        }
    }

    // Telegram HTML parse_mode requires these characters to be escaped.
    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    // Page URL and UTM can be long (fbclid etc.) — trim instead of rejecting the lead.
    private static string? Cut(string? value, int max) => value?.Length > max ? value[..max] + "…" : value;

    public class RutimLeadDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = null!;

        [MaxLength(200)]
        public string? Shop { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(100)]
        public string? Model { get; set; }

        [MaxLength(10)]
        public string? Lang { get; set; }

        public string? Page { get; set; }

        public string? Utm { get; set; }
    }
}
