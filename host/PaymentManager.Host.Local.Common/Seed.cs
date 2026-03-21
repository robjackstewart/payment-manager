using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;

namespace PaymentManager.Host.Local.Common;

public static class Seed
{
    // Stable GUIDs so seeds are idempotent
    // DefaultUserId matches DefaultUserService.DefaultUserId in the WebApi
    public static class UserIds
    {
        public static readonly Guid Default = Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    public static class PaymentSourceIds
    {
        public static readonly Guid Visa = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000001");
        public static readonly Guid Debit = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000002");
        public static readonly Guid Amex = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000003");
        public static readonly Guid Savings = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000004");
        public static readonly Guid Mastercard = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000005");
    }

    public static class PayeeIds
    {
        public static readonly Guid Netflix = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000001");
        public static readonly Guid Spotify = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000002");
        public static readonly Guid Landlord = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000003");
        public static readonly Guid ElectricCompany = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000004");
        public static readonly Guid GymMembership = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000005");
        public static readonly Guid Insurance = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000006");
        public static readonly Guid Dentist = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000007");
        public static readonly Guid CarService = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000008");
    }

    public static class PaymentIds
    {
        public static readonly Guid Netflix = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000001");
        public static readonly Guid Rent = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000002");
        public static readonly Guid Electric = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000003");
        public static readonly Guid Gym = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000004");
        public static readonly Guid Spotify = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000005");
        public static readonly Guid Insurance = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000006");
        public static readonly Guid Dentist = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000007");
        public static readonly Guid Rent2 = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000008");
        public static readonly Guid Netflix2 = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000009");
        public static readonly Guid CarService = Guid.Parse("f6f6f6f6-0000-0000-0000-00000000000a");
    }

    // The default user is seeded via EF HasData migration; no need to insert here.
    public static IReadOnlyList<User> Users => [];

    public static IReadOnlyList<PaymentSource> PaymentSources =>
    [
        new() { Id = PaymentSourceIds.Visa, UserId = UserIds.Default, Name = "Visa ending 4242" },
        new() { Id = PaymentSourceIds.Debit, UserId = UserIds.Default, Name = "Barclays Debit ending 9876" },
        new() { Id = PaymentSourceIds.Amex, UserId = UserIds.Default, Name = "Amex Gold ending 1111" },
        new() { Id = PaymentSourceIds.Savings, UserId = UserIds.Default, Name = "Chase Savings ending 5555" },
        new() { Id = PaymentSourceIds.Mastercard, UserId = UserIds.Default, Name = "Mastercard ending 3333" },
    ];

    public static IReadOnlyList<Payee> Payees =>
    [
        new() { Id = PayeeIds.Netflix, UserId = UserIds.Default, Name = "Netflix" },
        new() { Id = PayeeIds.Spotify, UserId = UserIds.Default, Name = "Spotify" },
        new() { Id = PayeeIds.Landlord, UserId = UserIds.Default, Name = "Greenfield Properties" },
        new() { Id = PayeeIds.ElectricCompany, UserId = UserIds.Default, Name = "City Power & Light" },
        new() { Id = PayeeIds.GymMembership, UserId = UserIds.Default, Name = "FitLife Gym" },
        new() { Id = PayeeIds.Insurance, UserId = UserIds.Default, Name = "SafeGuard Insurance" },
        new() { Id = PayeeIds.Dentist, UserId = UserIds.Default, Name = "Bright Smile Dental" },
        new() { Id = PayeeIds.CarService, UserId = UserIds.Default, Name = "AutoCare MOT & Service" },
    ];

    public static IReadOnlyList<Payment> Payments =>
    [
        new()
        {
            Id = PaymentIds.Netflix, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Visa, PayeeId = PayeeIds.Netflix,
            Amount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.Rent, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Debit, PayeeId = PayeeIds.Landlord,
            Amount = 1200.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1), EndDate = new DateOnly(2026, 2, 28),
        },
        new()
        {
            Id = PaymentIds.Electric, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Debit, PayeeId = PayeeIds.ElectricCompany,
            Amount = 85.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.Gym, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Visa, PayeeId = PayeeIds.GymMembership,
            Amount = 420.00m, Currency = "USD", Frequency = PaymentFrequency.Annually,
            StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2027, 6, 1),
        },
        new()
        {
            Id = PaymentIds.Spotify, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Amex, PayeeId = PayeeIds.Spotify,
            Amount = 11.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 2, 1), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.Insurance, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Savings, PayeeId = PayeeIds.Insurance,
            Amount = 960.00m, Currency = "USD", Frequency = PaymentFrequency.Annually,
            StartDate = new DateOnly(2025, 4, 15), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.Dentist, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Amex, PayeeId = PayeeIds.Dentist,
            Amount = 275.00m, Currency = "USD", Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 7, 10), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.Rent2, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.Landlord,
            Amount = 950.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 12, 31),
        },
        new()
        {
            Id = PaymentIds.Netflix2, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.Netflix,
            Amount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1), EndDate = null,
        },
        new()
        {
            Id = PaymentIds.CarService, UserId = UserIds.Default,
            PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.CarService,
            Amount = 389.50m, Currency = "USD", Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 9, 20), EndDate = null,
        },
    ];
}