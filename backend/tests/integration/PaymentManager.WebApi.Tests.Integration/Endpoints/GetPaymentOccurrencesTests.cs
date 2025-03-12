using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Domain.Entities.PaymentSchedule;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class GetPaymentOccurrencesTests
{
    internal record ExpectedResponse(ExpectedPaymentOccurrenceDto[] Occurrences);
    internal record ExpectedPaymentOccurrenceDto(Guid Id, string Name, string? Description, decimal Amount, DateOnly Date, string Source);

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnAllPaymentsForDefaultUser_When_NoDateRangeSpecified()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            Name = "PaymentSource1",
            Description = "PaymentSource1 Description",
            UserId = User.DefaultUser.Id,

        };
        var oneTimePayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.10m,
            Name = "One time payment",
            Description = "A payment that happens once",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = null,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Once,
                Every = 0
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var dailyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 220.20m,
            Name = "Daily Payment",
            Description = "A payment thsat occurs across three consecutive days",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Daily,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var weeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 330.30m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var biWeeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 440.40m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 2
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        var monthlyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 550.50m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across 6 consecutive months",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Monthly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var annualPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 660.60m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across two consecutive years",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Annually,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        context.Payments.AddRange(oneTimePayment, dailyPayment, weeklyPayment, biWeeklyPayment, monthlyPayment, annualPayment);
        await context.SaveChanges(cancellationToken);

        var ExpectedPaymentOccurrenceDtos = new[]
        {
            new ExpectedPaymentOccurrenceDto(oneTimePayment.Id, oneTimePayment.Name, oneTimePayment.Description, oneTimePayment.Amount, oneTimePayment.Schedule.StartDate, oneTimePayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(1), dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.EndDate.Value, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(7), weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.EndDate.Value, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.StartDate, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.EndDate.Value, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate, monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(1), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(2), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(3), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(4), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(5), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.EndDate.Value, monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.StartDate, annualPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.StartDate.AddYears(1), annualPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.EndDate.Value, annualPayment.Source!.Name),
        }.OrderBy(p => p.Date).ThenBy(p => p.Id).ToArray();

        var expectedResponse = new ExpectedResponse(ExpectedPaymentOccurrenceDtos);

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().BeEquivalentTo(expectedResponse);
    }

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnAllPaymentsForDefaultUserThatHaveAnOccurrenceAfterTheFRomDate_When_OnlyFromDateIsSpecified()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            Name = "PaymentSource1",
            Description = "PaymentSource1 Description",
            UserId = User.DefaultUser.Id,

        };
        var oneTimePayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.10m,
            Name = "One time payment",
            Description = "A payment that happens once",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = null,
                StartDate = startDate,
                Occurs = Frequency.Once,
                Every = 0
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var dailyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 220.20m,
            Name = "Daily Payment",
            Description = "A payment thsat occurs across three consecutive days",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartDate = startDate,
                Occurs = Frequency.Daily,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var weeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 330.30m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = startDate,
                Occurs = Frequency.Weekly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var biWeeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 440.40m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = startDate,
                Occurs = Frequency.Weekly,
                Every = 2
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        var monthlyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 550.50m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across 6 consecutive months",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                StartDate = startDate,
                Occurs = Frequency.Monthly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var annualPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 660.60m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across two consecutive years",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                StartDate = startDate,
                Occurs = Frequency.Annually,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        context.Payments.AddRange(oneTimePayment, dailyPayment, weeklyPayment, biWeeklyPayment, monthlyPayment, annualPayment);
        await context.SaveChanges(cancellationToken);

        var ExpectedPaymentOccurrenceDtos = new[]
        {
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(1), dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.EndDate.Value, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(7), weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.EndDate.Value, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.EndDate.Value, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(1), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(2), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(3), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(4), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(5), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.EndDate.Value, monthlyPayment.Source!.Name),

            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.StartDate.AddYears(1), annualPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.EndDate.Value, annualPayment.Source!.Name),
        }.OrderBy(p => p.Date).ThenBy(p => p.Id).ToArray();

        var expectedResponse = new ExpectedResponse(ExpectedPaymentOccurrenceDtos);

        // var t = ExpectedPaymentOccurrenceDtos.Where(p => p.Date >= DateOnly.FromDateTime(DateTime.UtcNow) && p.Date <= DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))).ToArray();

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences?from={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().BeEquivalentTo(expectedResponse, options => options);
    }

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnAllPaymentsThatHaveOccurencesPriorToTheToDateForDefaultUser_When_OnlyTheToDateIsProvided()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            Name = "PaymentSource1",
            Description = "PaymentSource1 Description",
            UserId = User.DefaultUser.Id,

        };
        var oneTimePayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.10m,
            Name = "One time payment",
            Description = "A payment that happens once",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = null,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Once,
                Every = 0
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var dailyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 220.20m,
            Name = "Daily Payment",
            Description = "A payment thsat occurs across three consecutive days",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Daily,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var weeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 330.30m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var biWeeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 440.40m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 2
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        var monthlyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 550.50m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across 6 consecutive months",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Monthly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var annualPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 660.60m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across two consecutive years",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Annually,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        context.Payments.AddRange(oneTimePayment, dailyPayment, weeklyPayment, biWeeklyPayment, monthlyPayment, annualPayment);
        await context.SaveChanges(cancellationToken);

        var ExpectedPaymentOccurrenceDtos = new[]
        {
            new ExpectedPaymentOccurrenceDto(oneTimePayment.Id, oneTimePayment.Name, oneTimePayment.Description, oneTimePayment.Amount, oneTimePayment.Schedule.StartDate, oneTimePayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(1), dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.EndDate.Value, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(7), weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.EndDate.Value, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.StartDate, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.EndDate.Value, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate, monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(1), monthlyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(annualPayment.Id, annualPayment.Name, annualPayment.Description, annualPayment.Amount, annualPayment.Schedule.StartDate, annualPayment.Source!.Name),
        }.OrderBy(p => p.Date).ThenBy(p => p.Id).ToArray();

        var expectedResponse = new ExpectedResponse(ExpectedPaymentOccurrenceDtos);

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences?to={DateTime.UtcNow.AddMonths(1):yyyy-MM-dd}", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().BeEquivalentTo(expectedResponse);
    }

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnAllPaymentsWithinTheDateRangeForTheDefaultUser_When_BothToAndFromAreSpecified()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            Name = "PaymentSource1",
            Description = "PaymentSource1 Description",
            UserId = User.DefaultUser.Id,

        };
        var oneTimePayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.10m,
            Name = "One time payment",
            Description = "A payment that happens once",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = null,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Once,
                Every = 0
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var dailyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 220.20m,
            Name = "Daily Payment",
            Description = "A payment thsat occurs across three consecutive days",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Daily,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var weeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 330.30m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var biWeeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 440.40m,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Weekly,
                Every = 2
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        var monthlyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 550.50m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across 6 consecutive months",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Monthly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var annualPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 660.60m,
            Name = "Monthly Payment",
            Description = "A payment that occurs across two consecutive years",
            UserId = User.DefaultUser.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Occurs = Frequency.Annually,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        context.Payments.AddRange(oneTimePayment, dailyPayment, weeklyPayment, biWeeklyPayment, monthlyPayment, annualPayment);
        await context.SaveChanges(cancellationToken);

        var ExpectedPaymentOccurrenceDtos = new[]
        {
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(1), dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.EndDate.Value, dailyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(7), weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.EndDate.Value, weeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.EndDate.Value, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentOccurrenceDto(monthlyPayment.Id, monthlyPayment.Name, monthlyPayment.Description, monthlyPayment.Amount, monthlyPayment.Schedule.StartDate.AddMonths(1), monthlyPayment.Source!.Name),
        }.OrderBy(p => p.Date).ThenBy(p => p.Id).ToArray();

        var expectedResponse = new ExpectedResponse(ExpectedPaymentOccurrenceDtos);

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences?to={DateTime.UtcNow.AddMonths(1):yyyy-MM-dd}&from={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().BeEquivalentTo(expectedResponse);
    }

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnBadRequestWithProblemDetails_When_ToIsNotParsableToDateOnly()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences?to={Guid.NewGuid()}", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Invalid request");
        body!.Detail.Should().Be("One or more validation errors occurred.");
        body!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]> { { "To", ["Must be a valid date in the format 'yyyy-MM-dd'"] } });
    }

    [Test]
    public async Task PaymentsEndpoint_Should_ReturnBadRequestWithProblemDetails_When_FromIsNotParsableToDateOnly()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/occurrences?from={Guid.NewGuid()}", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Invalid request");
        body!.Detail.Should().Be("One or more validation errors occurred.");
        body!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]> { { "From", ["Must be a valid date in the format 'yyyy-MM-dd'"] } });
    }
}
