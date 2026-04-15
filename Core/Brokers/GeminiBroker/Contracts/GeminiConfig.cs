using System.ComponentModel.DataAnnotations;

namespace Core.Brokers.GeminiBroker.Contracts;

public class GeminiConfig
{
    [Required] public string ApiKey { get; set; } = null!;
}