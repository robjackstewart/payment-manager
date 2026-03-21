namespace PaymentManager.Infrastructure;

public record Configuration
{
    public required string DatabaseConnectionString { get; init; }
}
