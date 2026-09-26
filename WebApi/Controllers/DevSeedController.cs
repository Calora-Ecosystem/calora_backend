using Core.Attributes;
using Core.Enums;
using Core.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>
/// VAQTINCHALIK (faqat test uchun): haftalik hisobotni tekshirish uchun userga
/// 2 haftalik ovqat, qadam va suv datasini qo'shadi / o'chiradi. Faqat SuperAdmin.
/// </summary>
[ApiController]
[Route("dev/seed")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public class DevSeedController(WeeklyReportSeedService seedService) : AuthorizedController
{
    /// <summary>
    /// <c>weekStart</c> haftasi va undan oldingi hafta (berilmasa — o'tgan hafta).
    /// <c>phone</c> — oxirgi 9 raqami bo'yicha, masalan <c>977410520</c>.
    /// </summary>
    [HttpPost("weekly")]
    public async Task<Wrapper> Seed([FromQuery] string phone, [FromQuery] DateTime? weekStart) =>
        (await seedService.Seed(phone, weekStart), 200);

    /// <summary>Seed qo'shgan ovqat, qadam va suvni o'chiradi.</summary>
    [HttpDelete("weekly")]
    public async Task<Wrapper> Clear([FromQuery] string phone, [FromQuery] DateTime? weekStart) =>
        (await seedService.Clear(phone, weekStart), 200);
}
