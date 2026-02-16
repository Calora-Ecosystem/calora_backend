using System.Text.Json.Serialization;

namespace Core.Services.Billing.Rc.Contracts;

public record RcRequest
{
    public Event Event { get; set; } = null!;
    public string ApiVersion { get; set; } = null!;
}

public class Event
{
    public long EventTimestampMs { get; set; }
    public string ProductId { get; set; } = null!;
    public SubscriberAttributes SubscriberAttribute { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string AppId { get; set; } = null!;
}

public class SubscriberAttributes
{
    [JsonPropertyName("email")] public AttrItem? Email { get; set; } = null!;
    [JsonPropertyName("order_id")] public AttrItem? OrderId { get; set; } = null!;
}

public class AttrItem
{
    public long UpdatedAtMs { get; set; }
    public string Value { get; set; } = null!;
}