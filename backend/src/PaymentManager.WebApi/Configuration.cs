namespace PaymentManager.WebApi;

public record class Configuration
{
    public required ConnectionStringsConfiguration ConnectionStrings { get; init; }
    public string? BasePath { get; init; }

    public record ConnectionStringsConfiguration
    {
        public required string PaymentManager { get; init; }
    }
}
