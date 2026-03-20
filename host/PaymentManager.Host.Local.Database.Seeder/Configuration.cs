namespace PaymentManager.Host.Local.Database.Seeder;

public record class Configuration
{
    public required string DatabaseConnectionString { get; init; }
}
