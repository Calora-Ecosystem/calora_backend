using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Date))]
public class UserDaily : UserNormGeneral
{
    public DateTime Date { get; set; }
}