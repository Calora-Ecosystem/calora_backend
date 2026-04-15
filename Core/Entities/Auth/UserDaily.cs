using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Date))]
[Index(nameof(UserId), nameof(Date), nameof(Metric), IsUnique = true)]
public class UserDaily : UserNormGeneral
{
    public DateTime Date { get; set; }
}