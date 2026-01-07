namespace Core.Services.Billing.Payme.Contracts;

public class ErrorResponseDto : BaseResponseDto
{
    public ErrorDto Error { get; set; } = null!;
}

public class ErrorDto
{
    public int Code { get; set; }
    public MessageDto Message { get; set; } = null!;
    public string? Data { get; set; }
}

public class MessageDto
{
    public string Ru { get; set; } = null!;
    public string Uz { get; set; } = null!;
    public string En { get; set; } = null!;
}