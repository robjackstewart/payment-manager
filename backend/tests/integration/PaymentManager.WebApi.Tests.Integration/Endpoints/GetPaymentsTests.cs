using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Domain.Entities.PaymentSchedule;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class GetPaymentsTests
{
    internal record ExpectedResponse(ExpectedPaymentDto[] Payments);
    internal record ExpectedPaymentDto(Guid Id, string Name, string? Description, decimal Amount, DateOnly Date, string Source);
    [Test]
    public async Task PaymentsEndpoint_Should_ReturnAllPayments_When_NoDateRangeSpecified()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "User1"
        };
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            Name = "PaymentSource1",
            Description = "PaymentSource1 Description",
            UserId = user.Id,
            User = user
        };
        var oneTimePayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100,
            Name = "One time payment",
            Description = "A payment that happens once",
            UserId = user.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = null,
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                Occurs = Frequency.Once,
                Every = 0
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var dailyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100,
            Name = "Daily Payment",
            Description = "A payment thsat occurs across three consecutive days",
            UserId = user.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                Occurs = Frequency.Daily,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var weeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = user.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                Occurs = Frequency.Weekly,
                Every = 1
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };
        var biWeeklyPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100,
            Name = "Weekly Payment",
            Description = "A payment thsat occurs across three consecutive weeks",
            UserId = user.Id,
            Schedule = new PaymentSchedule
            {
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(14)),
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                Occurs = Frequency.Weekly,
                Every = 2
            },
            Source = paymentSource,
            SourceId = paymentSource.Id
        };

        context.Payments.AddRange(oneTimePayment, dailyPayment, weeklyPayment, biWeeklyPayment);
        await context.SaveChanges(cancellationToken);

        var expectedPaymentDtos = new[]
        {
            new ExpectedPaymentDto(oneTimePayment.Id, oneTimePayment.Name, oneTimePayment.Description, oneTimePayment.Amount, oneTimePayment.Schedule.StartDate, oneTimePayment.Source!.Name),
            new ExpectedPaymentDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate, dailyPayment.Source!.Name),
            new ExpectedPaymentDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(1), dailyPayment.Source!.Name),
            new ExpectedPaymentDto(dailyPayment.Id, dailyPayment.Name, dailyPayment.Description, dailyPayment.Amount, dailyPayment.Schedule.StartDate.AddDays(2), dailyPayment.Source!.Name),
            new ExpectedPaymentDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate, weeklyPayment.Source!.Name),
            new ExpectedPaymentDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(7), weeklyPayment.Source!.Name),
            new ExpectedPaymentDto(weeklyPayment.Id, weeklyPayment.Name, weeklyPayment.Description, weeklyPayment.Amount, weeklyPayment.Schedule.StartDate.AddDays(14), weeklyPayment.Source!.Name),
            new ExpectedPaymentDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.StartDate, biWeeklyPayment.Source!.Name),
            new ExpectedPaymentDto(biWeeklyPayment.Id, biWeeklyPayment.Name, biWeeklyPayment.Description, biWeeklyPayment.Amount, biWeeklyPayment.Schedule.StartDate.AddDays(14), biWeeklyPayment.Source!.Name),
        }.OrderBy(p => p.Date).ThenBy(p => p.Id).ToArray();

        var expectedResponse = new ExpectedResponse(expectedPaymentDtos);

        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments", cancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.ShouldBeEquivalentTo(expectedResponse);
    }
}
