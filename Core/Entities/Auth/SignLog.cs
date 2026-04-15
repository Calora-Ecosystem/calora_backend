using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(UserId))]
[Index(nameof(DeviceId))]
[Index(nameof(SignAt))]
public class SignLog : ModelBase<long>
{
    public long UserId { get; set; }
    public long DeviceId { get; set; }
    public DateTime SignAt { get; set; }
}