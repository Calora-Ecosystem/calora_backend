using Core.Enums;

namespace Core.Services.User.Contracts;

public class CreateUserExtraDto
{  
    public string Name { get; set; } = null!;
    public double Weight { get; set; }
    public double Height { get; set; }
    public double Bmi { get; set; }
    public EnumGender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Photo { get; set; }
    public List<long> PurposeIds { get; set; } = new();
}

public class UpdateUserExtraDto : CreateUserExtraDto
{
    public long Id { get; set; }
}