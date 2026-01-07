using System.Text.Json;

namespace Core.Services.Billing.Payme.Contracts;

public class BaseRequest
{
    public int Id { get; set; }
    public string Method { get; set; } = null!;
    public JsonElement Params { get; set; }
}