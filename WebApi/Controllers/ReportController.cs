using Core.Attributes;
using Core.Enums;
using Core.Services.Reports;
using Core.Services.Reports.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>
/// Userning haftalik hisoboti (dushanba kuni pop-up/story sifatida ko'rsatiladi).
/// </summary>
[ApiController]
[Route("reports")]
[RoleAuthorize(EnumRole.User)]
public class ReportController(WeeklyReportService weeklyReportService) : AuthorizedController
{
    /// <summary>
    /// <c>weekStart</c> tushgan haftaning (Du–Ya) hisoboti. Berilmasa — o'tgan hafta.
    /// </summary>
    [HttpGet("weekly")]
    [ProducesResponseType<WrapperGeneric<WeeklyReportDto>>(200)]
    public async Task<Wrapper> Weekly([FromQuery] DateTime? weekStart) =>
        (await weeklyReportService.GetWeekly(this.UserId, weekStart), 200);
}
