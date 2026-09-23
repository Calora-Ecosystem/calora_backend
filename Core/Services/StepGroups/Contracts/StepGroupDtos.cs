using System.ComponentModel.DataAnnotations;

namespace Core.Services.StepGroups.Contracts;

public class CreateStepGroupDto
{
    [Required, MinLength(1), MaxLength(100)] public string Name { get; set; } = null!;
}

public class JoinStepGroupDto
{
    [Required, MaxLength(20)] public string Code { get; set; } = null!;
}

public class StepGroupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string InviteCode { get; set; } = null!;
    public long OwnerId { get; set; }

    /// <summary>Joriy user guruh admini (egasi)mi.</summary>
    public bool IsOwner { get; set; }

    public int MemberCount { get; set; }

    /// <summary>Tanlangan davrda guruh a'zolarining jami qadamlari.</summary>
    public double TotalSteps { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class StepGroupMemberDto
{
    public long UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Photo { get; set; }

    /// <summary>Tanlangan davrdagi qadamlar.</summary>
    public double Steps { get; set; }

    /// <summary>Guruh ichidagi o'rin (1 dan).</summary>
    public int Index { get; set; }

    public bool IsMe { get; set; }
    public bool IsOwner { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class StepGroupDetailDto : StepGroupDto
{
    /// <summary>Qadamlar bo'yicha kamayish tartibida (reyting).</summary>
    public List<StepGroupMemberDto> Members { get; set; } = [];
}
