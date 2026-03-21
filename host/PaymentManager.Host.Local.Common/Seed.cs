using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;

namespace PaymentManager.Host.Local.Common;

public static class Seed
{
    // Stable GUIDs so seeds are idempotent
    public static class UserIds
    {
        public static readonly Guid Alice = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
        public static readonly Guid Bob = Guid.Parse("b2b2b2b2-0000-0000-0000-000000000002");
        public static readonly Guid Charlie = Guid.Parse("c3c3c3c3-0000-0000-0000-000000000003");
    }

    public static class PaymentSourceIds
    {
        public static readonly Guid AliceVisa = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000001");
        public static readonly Guid AliceDebit = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000002");
        public static readonly Guid BobAmex = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000003");
        public static readonly Guid BobSavings = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000004");
        public static readonly Guid CharlieMastercard = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000005");
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
        public static readonly Guid AliceNetflix = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000001");
        public static readonly Guid AliceRent = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000002");
        public static readonly Guid AliceElectric = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000003");
        public static readonly Guid AliceGym = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000004");
        public static readonly Guid BobSpotify = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000005");
        public static readonly Guid BobInsurance = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000006");
        public static readonly Guid BobDentist = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000007");
        public static readonly Guid CharlieRent = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000008");
        public static readonly Guid CharlieNetflix = Guid.Parse("f6f6f6f6-0000-0000-0000-000000000009");
        public static readonly Guid CharlieCarService = Guid.Parse("f6f6f6f6-0000-0000-0000-00000000000a");
    }

    public static IReadOnlyList<User> Users =>
    [
        new() { Id = UserIds.Alice, Name = "Alice Johnson" },
        new() { Id = UserIds.Bob, Name = "Bob Smith" },
        new() { Id = UserIds.Charlie, Name = "Charlie Brown" },
    ];

    public static IReadOnlyList<PaymentSource> PaymentSources =>
    [
        new() { Id = PaymentSourceIds.AliceVisa, UserId = UserIds.Alice, Name = "Visa ending 4242" },
        new() { Id = PaymentSourceIds.AliceDebit, UserId = UserIds.Alice, Name = "Barclays Debit ending 9876" },
        new() { Id = PaymentSourceIds.BobAmex, UserId = UserIds.Bob, Name = "Amex Gold ending 1111" },
        new() { Id = PaymentSourceIds.BobSavings, UserId = UserIds.Bob, Name = "Chase Savings ending 5555" },
        new() { Id = PaymentSourceIds.CharlieMastercard, UserId = UserIds.Charlie, Name = "Mastercard ending 3333" },
    ];

    public static IReadOnlyList<Payee> Payees =>
    [
        new() { Id = PayeeIds.Netflix, UserId = UserIds.Alice, Name = "Netflix" },
        new() { Id = PayeeIds.Spotify, UserId = UserIds.Bob, Name = "Spotify" },
        new() { Id = PayeeIds.Landlord, UserId = UserIds.Alice, Name = "Greenfield Properties" },
        new() { Id = PayeeIds.ElectricCompany, UserId = UserIds.Alice, Name = "City Power & Light" },
        new() { Id = PayeeIds.GymMembership, UserId = UserIds.Alice, Name = "FitLife Gym" },
        new() { Id = PayeeIds.Insurance, UserId = UserIds.Bob, Name = "SafeGuard Insurance" },
        new() { Id = PayeeIds.Dentist, UserId = UserIds.Bob, Name = "Bright Smile Dental" },
        new() { Id = PayeeIds.CarService, UserId = UserIds.Charlie, Name = "AutoCare MOT & Service" },
    ];

    public static IReadOnlyList<Payment> Payments =>
    [
        // Alice — monthly Netflix subscription via Visa
        new()
        {
            Id = PaymentIds.AliceNetflix, UserId = UserIds.Alice,
            PaymentSourceId = PaymentSourceIds.AliceVisa, PayeeId = PayeeIds.Netflix,
            Amount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = null,
        },
        // Alice — monthly rent via debit
        new()
        {
            Id = PaymentIds.AliceRent, UserId = UserIds.Alice,
            PaymentSourceId = PaymentSourceIds.AliceDebit, PayeeId = PayeeIds.Landlord,
            Amount = 1200.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1), EndDate = new DateOnly(2026, 2, 28),
        },
        // Alice — monthly electric bill via debit
        new()
        {
            Id = PaymentIds.AliceElectric, UserId = UserIds.Alice,
            PaymentSourceId = PaymentSourceIds.AliceDebit, PayeeId = PayeeIds.ElectricCompany,
            Amount = 85.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = null,
        },
        // Alice — annual gym membership via Visa
        new()
        {
            Id = PaymentIds.AliceGym, UserId = UserIds.Alice,
            PaymentSourceId = PaymentSourceIds.AliceVisa, PayeeId = PayeeIds.GymMembership,
            Amount = 420.00m, Currency = "USD", Frequency = PaymentFrequency.Annually,
            StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2027, 6, 1),
        },
        // Bob — monthly Spotify via Amex
        new()
        {
            Id = PaymentIds.BobSpotify, UserId = UserIds.Bob,
            PaymentSourceId = PaymentSourceIds.BobAmex, PayeeId = PayeeIds.Spotify,
            Amount = 11.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 2, 1), EndDate = null,
        },
        // Bob — annual insurance via savings
        new()
        {
            Id = PaymentIds.BobInsurance, UserId = UserIds.Bob,
            PaymentSourceId = PaymentSourceIds.BobSavings, PayeeId = PayeeIds.Insurance,
            Amount = 960.00m, Currency = "USD", Frequency = PaymentFrequency.Annually,
            StartDate = new DateOnly(2025, 4, 15), EndDate = null,
        },
        // Bob — one-off dentist visit via Amex
        new()
        {
            Id = PaymentIds.BobDentist, UserId = UserIds.Bob,
            PaymentSourceId = PaymentSourceIds.BobAmex, PayeeId = PayeeIds.Dentist,
            Amount = 275.00m, Currency = "USD", Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 7, 10), EndDate = null,
        },
        // Charlie — monthly rent via Mastercard
        new()
        {
            Id = PaymentIds.CharlieRent, UserId = UserIds.Charlie,
            PaymentSourceId = PaymentSourceIds.CharlieMastercard, PayeeId = PayeeIds.Landlord,
            Amount = 950.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 12, 31),
        },
        // Charlie — monthly Netflix via Mastercard
        new()
        {
            Id = PaymentIds.CharlieNetflix, UserId = UserIds.Charlie,
            PaymentSourceId = PaymentSourceIds.CharlieMastercard, PayeeId = PayeeIds.Netflix,
            Amount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1), EndDate = null,
        },
        // Charlie — one-off car service via Mastercard
        new()
        {
            Id = PaymentIds.CharlieCarService, UserId = UserIds.Charlie,
            PaymentSourceId = PaymentSourceIds.CharlieMastercard, PayeeId = PayeeIds.CarService,
            Amount = 389.50m, Currency = "USD", Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 9, 20), EndDate = null,
        },
    ];
}
