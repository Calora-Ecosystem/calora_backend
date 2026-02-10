namespace Core.Services.Ref.Contracts;

public record CheckDto
{
    public string Version { get; set; } = null!;
    public bool IsActive { get; set; }
    public long Id { get; set; }
}