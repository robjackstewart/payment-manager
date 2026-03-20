namespace PaymentManager.Host.Local.Database.Seeder;

public record class Configuration
{
    public required ConnectionStringsConfiguration ConnectionStrings { get; init; }

    public record ConnectionStringsConfiguration
    {
        public required string PaymentManager { get; init; }
    }
}
