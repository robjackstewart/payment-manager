namespace PaymentManager.WebApi;

public record class Configuration
{
    public required CorsConfiguration Cors { get; init; }
    public required ConnectionStringsConfiguration ConnectionStrings { get; init; }
    public record CorsConfiguration
    {
        public required string[] AllowedOrigins { get; init; }
    }

    public record ConnectionStringsConfiguration
    {
        public required string PaymentManager { get; init; }
    }
}
