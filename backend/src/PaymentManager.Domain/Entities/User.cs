namespace PaymentManager.Domain.Entities;

public record User()
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }

    public static User DefaultUser => new() { Id = new Guid("ae25b45e-63af-4b89-a8e8-2bb3e142f06d"), Name = "Default User" };
}
