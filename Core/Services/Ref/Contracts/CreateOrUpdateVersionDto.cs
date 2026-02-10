namespace Core.Services.Ref.Contracts;

public class CreateOrUpdateVersionDto
{
    public long? Id { get; set; }
    public string? Key { get; set; } = null!;
    public bool IsActive { get; set; }
}