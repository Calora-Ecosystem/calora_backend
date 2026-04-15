namespace Core.Brokers.EmailBroker;

public class EmailConfig
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string From { get; set; }
    public required string Host { get; set; }
    public required int Port { get; set; }
}