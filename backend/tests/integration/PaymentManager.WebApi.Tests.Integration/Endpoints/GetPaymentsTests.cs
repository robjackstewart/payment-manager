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
    public async Task PaymentsEndpoint_Should_ReturnAllPaymentsFromFromDate_When_OnlyFromDateIsProvided()
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
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100,
            Name = "Payment1",
            Description = "Payment1 Description",
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

        context.Payments.Add(payment);
        await context.SaveChanges(cancellationToken);

        var expectedResponse = new ExpectedResponse(
        [
            new ExpectedPaymentDto(payment.Id, payment.Name, payment.Description, payment.Amount, payment.Schedule.StartDate, payment.Source!.Name)
        ]);

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
