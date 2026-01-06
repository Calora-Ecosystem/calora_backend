using System.ComponentModel.DataAnnotations;

namespace Core.Brokers.EskizBroker;

public class EskizConfig
{
    [Required] public string BaseUrl { get; set; } = null!;
    [Required] public string Login { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}