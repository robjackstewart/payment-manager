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

    public static class PayerGroupIds
    {
        public static readonly Guid TheFlat = Guid.Parse("b2b2b2b2-0000-0000-0000-000000000001");
    }

    public static class PersonIds
    {
        public static readonly Guid CurrentUser = Guid.Parse("c3c3c3c3-0000-0000-0000-000000000000");
        public static readonly Guid Jane = Guid.Parse("c3c3c3c3-0000-0000-0000-000000000001");
        public static readonly Guid Sam = Guid.Parse("c3c3c3c3-0000-0000-0000-000000000002");
    }

    public static class PaymentSourceIds
    {
        public static readonly Guid Visa = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000001");
        public static readonly Guid Debit = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000002");
        public static readonly Guid Amex = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000003");
        public static readonly Guid Savings = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000004");
        public static readonly Guid Mastercard = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000005");
        public static readonly Guid JointAccount = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000006");
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
        public static readonly Guid Employer = Guid.Parse("e5e5e5e5-0000-0000-0000-000000000009");
        public static readonly Guid JaneEmployer = Guid.Parse("e5e5e5e5-0000-0000-0000-00000000000a");
        public static readonly Guid SideGigClient = Guid.Parse("e5e5e5e5-0000-0000-0000-00000000000b");
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
        public static readonly Guid Salary = Guid.Parse("f6f6f6f6-0000-0000-0000-00000000000b");
        public static readonly Guid JaneSalary = Guid.Parse("f6f6f6f6-0000-0000-0000-00000000000c");
        public static readonly Guid SideIncome = Guid.Parse("f6f6f6f6-0000-0000-0000-00000000000d");
    }

    public static IReadOnlyList<User> Users => [];

    public static IReadOnlyList<PayerGroup> PayerGroups =>
    [
        new() { Id = PayerGroupIds.TheFlat, UserId = UserIds.Default, Name = "The Flat" },
    ];

    public static IReadOnlyList<Person> People =>
    [
        new() { Id = PersonIds.CurrentUser, UserId = UserIds.Default, Name = "Current User" },
        new() { Id = PersonIds.Jane, UserId = UserIds.Default, Name = "Jane Doe" },
        new() { Id = PersonIds.Sam, UserId = UserIds.Default, Name = "Sam Wilson" },
    ];

    public static IReadOnlyList<PayerGroupMember> PayerGroupMembers =>
    [
        new() { PayerGroupId = PayerGroupIds.TheFlat, PersonId = PersonIds.CurrentUser },
        new() { PayerGroupId = PayerGroupIds.TheFlat, PersonId = PersonIds.Jane },
        new() { PayerGroupId = PayerGroupIds.TheFlat, PersonId = PersonIds.Sam },
    ];

    public static IReadOnlyList<PaymentSource> PaymentSources =>
    [
        new() { Id = PaymentSourceIds.Visa, UserId = UserIds.Default, Name = "Visa ending 4242" },
        new() { Id = PaymentSourceIds.Debit, UserId = UserIds.Default, Name = "Barclays Debit ending 9876" },
        new() { Id = PaymentSourceIds.Amex, UserId = UserIds.Default, Name = "Amex Gold ending 1111" },
        new() { Id = PaymentSourceIds.Savings, UserId = UserIds.Default, Name = "Chase Savings ending 5555" },
        new() { Id = PaymentSourceIds.Mastercard, UserId = UserIds.Default, Name = "Mastercard ending 3333" },
        new() { Id = PaymentSourceIds.JointAccount, UserId = UserIds.Default, Name = "Joint Current Account" },
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
        new() { Id = PayeeIds.Employer, UserId = UserIds.Default, Name = "Acme Corp" },
        new() { Id = PayeeIds.JaneEmployer, UserId = UserIds.Default, Name = "Bright Ideas Ltd" },
        new() { Id = PayeeIds.SideGigClient, UserId = UserIds.Default, Name = "Gig Co" },
    ];

    public static IReadOnlyList<Payment> Payments =>
    [
        new() { Id = PaymentIds.Netflix, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Visa, PayeeId = PayeeIds.Netflix, InitialAmount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 1, 1), EndDate = null, PayerGroupId = PayerGroupIds.TheFlat },
        new() { Id = PaymentIds.Rent, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Debit, PayeeId = PayeeIds.Landlord, InitialAmount = 1200.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 3, 1), EndDate = new DateOnly(2026, 2, 28), PayerGroupId = PayerGroupIds.TheFlat },
        new() { Id = PaymentIds.Electric, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Debit, PayeeId = PayeeIds.ElectricCompany, InitialAmount = 85.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 1, 1), EndDate = null, PayerGroupId = PayerGroupIds.TheFlat },
        new() { Id = PaymentIds.Gym, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Visa, PayeeId = PayeeIds.GymMembership, InitialAmount = 420.00m, Currency = "USD", Frequency = PaymentFrequency.Annually, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2027, 6, 1) },
        new() { Id = PaymentIds.Spotify, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Amex, PayeeId = PayeeIds.Spotify, InitialAmount = 11.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 2, 1), EndDate = null },
        new() { Id = PaymentIds.Insurance, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Savings, PayeeId = PayeeIds.Insurance, InitialAmount = 960.00m, Currency = "USD", Frequency = PaymentFrequency.Annually, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 4, 15), EndDate = null },
        new() { Id = PaymentIds.Dentist, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Amex, PayeeId = PayeeIds.Dentist, InitialAmount = 275.00m, Currency = "USD", Frequency = PaymentFrequency.Once, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 7, 10), EndDate = null },
        new() { Id = PaymentIds.Rent2, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.Landlord, InitialAmount = 950.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 12, 31) },
        new() { Id = PaymentIds.Netflix2, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.Netflix, InitialAmount = 15.99m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 3, 1), EndDate = null },
        new() { Id = PaymentIds.CarService, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.Mastercard, PayeeId = PayeeIds.CarService, InitialAmount = 389.50m, Currency = "USD", Frequency = PaymentFrequency.Once, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 9, 20), EndDate = null },

        // Incoming
        new() { Id = PaymentIds.Salary, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.JointAccount, PayeeId = PayeeIds.Employer, InitialAmount = 3200.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Incoming, StartDate = new DateOnly(2025, 1, 1), EndDate = null, Description = "Salary" },
        new() { Id = PaymentIds.JaneSalary, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.JointAccount, PayeeId = PayeeIds.JaneEmployer, InitialAmount = 2600.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Incoming, StartDate = new DateOnly(2025, 1, 1), EndDate = null, Description = "Jane's salary" },
        new() { Id = PaymentIds.SideIncome, UserId = UserIds.Default, PaymentSourceId = PaymentSourceIds.JointAccount, PayeeId = PayeeIds.SideGigClient, InitialAmount = 450.00m, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Incoming, StartDate = new DateOnly(2025, 5, 1), EndDate = null, Description = "Freelance side income" },
    ];

    public static IReadOnlyList<PaymentSplit> PaymentSplits =>
    [
        // Rent and electric are shared three ways with the flatmates; splits must sum to
        // exactly 100, so "Current User" carries the odd penny left over from the 33.33/33.33 split.
        new() { PaymentId = PaymentIds.Rent, PersonId = PersonIds.CurrentUser, Percentage = 33.34m },
        new() { PaymentId = PaymentIds.Rent, PersonId = PersonIds.Jane, Percentage = 33.33m },
        new() { PaymentId = PaymentIds.Rent, PersonId = PersonIds.Sam, Percentage = 33.33m },
        new() { PaymentId = PaymentIds.Electric, PersonId = PersonIds.CurrentUser, Percentage = 33.34m },
        new() { PaymentId = PaymentIds.Electric, PersonId = PersonIds.Jane, Percentage = 33.33m },
        new() { PaymentId = PaymentIds.Electric, PersonId = PersonIds.Sam, Percentage = 33.33m },
        // Netflix is split with Jane only.
        new() { PaymentId = PaymentIds.Netflix, PersonId = PersonIds.CurrentUser, Percentage = 50.00m },
        new() { PaymentId = PaymentIds.Netflix, PersonId = PersonIds.Jane, Percentage = 50.00m },
        // Ungrouped bills belong entirely to "Current User" — every payment needs at least one split.
        new() { PaymentId = PaymentIds.Gym, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.Spotify, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.Insurance, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.Dentist, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.Rent2, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.Netflix2, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.CarService, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        // Income belongs to whoever earns it.
        new() { PaymentId = PaymentIds.Salary, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.JaneSalary, PersonId = PersonIds.Jane, Percentage = 100.00m },
        new() { PaymentId = PaymentIds.SideIncome, PersonId = PersonIds.CurrentUser, Percentage = 100.00m },
    ];

    public static IReadOnlyList<EffectivePaymentValue> EffectivePaymentValues =>
    [
        new() { PaymentId = PaymentIds.Rent, EffectiveDate = new DateOnly(2026, 3, 1), Amount = 1250.00m },
        new() { PaymentId = PaymentIds.Electric, EffectiveDate = new DateOnly(2025, 9, 1), Amount = 95.00m },
        new() { PaymentId = PaymentIds.Salary, EffectiveDate = new DateOnly(2026, 1, 1), Amount = 3350.00m },
    ];
}
