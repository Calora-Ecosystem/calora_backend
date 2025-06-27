namespace Core.Brokers.EmailBroker;

public class EmailConfig
{
    public required string Login { get; set; }
    public required string Password { get; set; }
    public required string Name { get; set; }
    public required string Host { get; set; }
    public required int Port { get; set; }
}