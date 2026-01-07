namespace Core.Services.Billing.Payme.Contracts;

public class ResultResponseDto<T> : BaseResponseDto
{
    public T Result { get; set; } = default!;
}