using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Date))]
public class UserDaily : UserNorm
{
    public DateTime Date { get; set; }
}