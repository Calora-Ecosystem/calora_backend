using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Calora.Api.Controllers;

// =======================
// DTO (Request modeli)
// =======================
public record SendTicketRequest(
    string Email,
    string? FullName,
    string? Language // "uz", "ru", "en"
);

// =======================
// Service interface
// =======================
public interface IEmailService
{
    Task SendCaloraTicketAsync(string email, string? fullName, string? language);
}

// =======================
// Service implementation
// =======================
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config)
    {
        _config = config;

        // appsettings.json dagi Email bo'limidan o'qiyapmiz
        _fromEmail = _config["Email:FromEmail"]
                     ?? _config["Email:Username"]
                     ?? throw new InvalidOperationException("Email:FromEmail or Email:Username is not configured");

        _fromName = _config["Email:FromName"] ?? "Calora";
    }

    public async Task SendCaloraTicketAsync(string email, string? fullName, string? language)
    {
        var lang = NormalizeLanguage(language); // "uz", "ru", "en"
        var htmlBody = BuildHtml(fullName ?? "there", lang);

        using var message = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = GetSubjectByLanguage(lang),
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(email);

        using var client = new SmtpClient
        {
            Host = _config["Email:Host"]!,                     // smtp.gmail.com
            Port = int.Parse(_config["Email:Port"] ?? "587"),  // 587
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["Email:Username"],
                _config["Email:Password"]
            )
        };

        await client.SendMailAsync(message);
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        var lang = language.Trim().ToLowerInvariant();

        if (lang.StartsWith("uz")) return "uz";
        if (lang.StartsWith("ru")) return "ru";
        if (lang.StartsWith("en")) return "en";

        return "en";
    }

    private static string GetSubjectByLanguage(string lang)
    {
        return lang switch
        {
            "uz" => "Sizning Calora promo-kodingiz tayyor 🎟",
            "ru" => "Ваш промокод Calora готов 🎟",
            _ => "Your Calora promo code is ready 🎟"
        };
    }

    private static string BuildHtml(string fullName, string lang)
    {
        // Til bo‘yicha matnlar
        string headerTitle;
        string headerSubtitle;
        string hiLine;
        string body1;
        string body2;
        string ticketTitle;
        string ticketDescription;
        string perkLeft;
        string perkRight;
        string ctaText;
        string footerLine;

        switch (lang)
        {
            case "uz":
                headerTitle = "Calora kutish ro‘yxatiga xush kelibsiz 🎉";
                headerSubtitle = "1 oylik bepul Premium chiptangiz tayyor!";
                hiLine = $"Salom {fullName},";
                body1 =
                    "Calora kutish ro‘yxatiga qo‘shilganingiz uchun rahmat. " +
                    "Calora — ovqatlaringizni kuzatib boradigan, tanangiz holatini nazorat qiladigan " +
                    "va sog‘lom odatlarni ushlab turishingizga yordam beradigan sun’iy intellekt asosidagi sog‘lom turmush ilovasi.";
                body2 =
                    "Minnatdorchilik sifatida sizga maxsus <strong>promo-kod</strong> yuboryapmiz. " +
                    "Ushbu kod orqali ilova ishga tushirilgandan keyin <strong>Calora Premium’dan 1 oy davomida mutlaqo bepul</strong> foydalanishingiz mumkin.";
                ticketTitle = "Sizning Calora chiptangiz";
                ticketDescription =
                    "Ilova ishga tushirilgach, quyidagi promo-kod orqali <strong>1 oylik Calora Premium’ni bepul</strong> faollashtirishingiz mumkin:";
                perkLeft = "🔓 1 oy bepul Calora Premium";
                perkRight = "🎁 Faqat kutish ro‘yxatidagi foydalanuvchilar uchun";
                ctaText = "Yangiliklarni ko‘rish";
                footerLine =
                    "Siz bu xatni Calora kutish ro‘yxatiga yozilganingiz uchun oldingiz.";
                break;

            case "ru":
                headerTitle = "Добро пожаловать в список ожидания Calora 🎉";
                headerSubtitle = "Ваш билет на 1 месяц бесплатного Premium готов!";
                hiLine = $"Привет, {fullName},";
                body1 =
                    "Спасибо, что присоединились к списку ожидания <strong>Calora</strong>. " +
                    "Calora — это приложение для здорового образа жизни на основе ИИ, " +
                    "которое помогает отслеживать питание, состояние тела и прогресс к вашим целям.";
                body2 =
                    "В знак благодарности мы отправляем вам специальный <strong>промокод</strong>. " +
                    "С этим кодом вы сможете пользоваться <strong>Calora Premium совершенно бесплатно в течение 1 месяца</strong> после запуска приложения.";
                ticketTitle = "Ваш билет Calora";
                ticketDescription =
                    "После запуска приложения используйте этот промокод, чтобы активировать <strong>1 месяц Calora Premium бесплатно</strong>:";
                perkLeft = "🔓 1 месяц бесплатного Calora Premium";
                perkRight = "🎁 Эксклюзивно для участников списка ожидания";
                ctaText = "Смотреть обновления проекта";
                footerLine =
                    "Вы получили это письмо, потому что присоединились к списку ожидания Calora.";
                break;

            default: // en
                headerTitle = "Welcome to Calora Waitlist 🎉";
                headerSubtitle = "Your early access ticket is ready!";
                hiLine = $"Hi {fullName},";
                body1 =
                    "Thank you for joining the <strong>Calora</strong> waitlist. " +
                    "Calora is an <strong>AI-powered healthy living app</strong> that helps you track meals, " +
                    "monitor your body, and stay on track with your health goals — all in one place.";
                body2 =
                    "As a thank you, we’re sending you a special <strong>promo code</strong>. " +
                    "With this code, you’ll be able to use <strong>Calora Premium completely free for 1 month</strong> after the app launches.";
                ticketTitle = "Your Calora Ticket";
                ticketDescription =
                    "Use this promo code to unlock <strong>1 month of Calora Premium for free</strong> when the app launches:";
                perkLeft = "🔓 1 month free Calora Premium";
                perkRight = "🎁 Exclusive for waitlist members";
                ctaText = "View project updates";
                footerLine =
                    "You received this email because you joined the Calora waiting list.";
                break;
        }

        // HTML – sen yoqtirgan dizayn, faqat matnlar til bo‘yicha
        return $@"
<!DOCTYPE html>
<html lang=""en"" style=""margin:0; padding:0;"">
  <head>
    <meta charset=""UTF-8"" />
    <title>Calora Welcome Ticket</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  </head>
  <body style=""margin:0; padding:0; background-color:#f5f7fa; font-family: Arial, sans-serif;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f5f7fa; padding:24px 0;"">
      <tr>
        <td align=""center"">
          
          <!-- Card -->
          <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                 style=""max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.06);"">
            
            <!-- Header -->
            <tr>
              <td align=""center"" style=""background:linear-gradient(135deg,#8CC156,#b4df83); padding:24px 20px;"">
                <img src=""https://staging.calora.uz/api/file/images/B3-logo.jpg"" 
                     alt=""Calora Logo"" width=""40"" height=""40""
                     style=""display:block; border-radius:12px; margin-bottom:10px;"" />
                
                <h1 style=""margin:0; font-size:22px; line-height:1.4; color:#ffffff; font-weight:700;"">
                  {headerTitle}
                </h1>
                <p style=""margin:8px 0 0; font-size:14px; line-height:1.6; color:#fdfdfd;"">
                  {headerSubtitle}
                </p>
              </td>
            </tr>

            <!-- Body -->
            <tr>
              <td style=""padding:20px 24px 8px;"">
                <p style=""margin:0 0 12px; font-size:14px; line-height:1.6; color:#555555;"">
                  {hiLine}
                </p>
                <p style=""margin:0 0 10px; font-size:14px; line-height:1.6; color:#555555;"">
                  {body1}
                </p>
                <p style=""margin:0 0 16px; font-size:14px; line-height:1.6; color:#555555;"">
                  {body2}
                </p>
              </td>
            </tr>

            <!-- Ticket Block -->
            <tr>
              <td style=""padding:0 24px 20px;"">
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
                       style=""border-radius:14px; border:1px dashed #8CC156; background-color:#f9fff4;"">
                  
                  <tr>
                    <td style=""padding:16px 18px 10px;"">
                      
                      <p style=""margin:0 0 4px; font-size:12px; 
                                text-transform:uppercase; letter-spacing:1px; color:#8CC156; font-weight:700;"">
                        {ticketTitle}
                      </p>

                      <p style=""margin:0 0 8px; font-size:13px; color:#555555;"">
                        {ticketDescription}
                      </p>

                      <!-- FIXED PROMO CODE -->
                      <p style=""margin:0; font-size:26px; font-weight:800; 
                                letter-spacing:4px; color:#1b1b1b; text-align:center; padding:12px 0;"">
                        CALORA
                      </p>

                    </td>
                  </tr>

                  <tr>
                    <td style=""padding:10px 18px 12px; border-top:1px dashed #e0f0cd;"">
                      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                        <tr>
                          <td style=""font-size:12px; color:#777777; width:50%; padding-right:6px;"">
                            {perkLeft}
                          </td>
                          <td style=""font-size:12px; color:#777777; width:50%; text-align:right; padding-left:6px;"">
                            {perkRight}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>

            <!-- CTA -->
            <tr>
              <td align=""center"" style=""padding:0 24px 20px;"">
                <p style=""margin:0 0 10px; font-size:14px; line-height:1.6; color:#555555;"">
                  {(lang switch
        {
            "uz" => "Ilova ishga tushirilganda, promo-kodingizni faollashtirish uchun sizga yana xabar beramiz.",
            "ru" => "Мы сообщим вам, как только Calora запустится, чтобы вы могли активировать свой бесплатный месяц.",
            _ => "We’ll email you again as soon as Calora is live, so you can activate your free month."
        })}
                </p>

                <a href=""https://calora.uz""
                   style=""display:inline-block; padding:10px 22px; border-radius:999px;
                          background-color:#8CC156; text-decoration:none; font-size:14px; 
                          font-weight:600; color:#ffffff;"">
                  {ctaText}
                </a>
              </td>
            </tr>

            <!-- Footer -->
            <tr>
              <td style=""padding:14px 24px 18px; text-align:center; border-top:1px solid #f0f0f0;"">
                <p style=""margin:0 0 4px; font-size:11px; color:#999999;"">
                  {footerLine}
                </p>
                <p style=""margin:0; font-size:11px; color:#999999;"">
                  © 2025 Calora. All rights reserved.
                </p>
              </td>
            </tr>

          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
    }
}

// =======================
// Controller
// =======================
[ApiController]
public class TicketController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TicketController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // POST: send
    [HttpPost("send")]
    public async Task<IActionResult> SendTicket([FromBody] SendTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        await _emailService.SendCaloraTicketAsync(request.Email, request.FullName, request.Language);

        return Ok(new { message = "Ticket email sent" });
    }
}




