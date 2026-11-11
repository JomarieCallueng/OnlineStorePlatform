namespace Store.Shared.Common.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Prefetch for consumers (manual-ack back-pressure).</summary>
    public ushort Prefetch { get; set; } = 10;
}
