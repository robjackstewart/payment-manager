using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Endpoints;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class PaymentEndpointTests
{
    [Test]
    public async Task HandleCreate_Should_Return_CreatedResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var paymentSourceId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var amount = 100.50m;
        var currency = "USD";
        var frequency = PaymentFrequency.Monthly;
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 12, 31);
        var request = new PaymentEndpoints.CreateRequest(userId, paymentSourceId, payeeId, amount, currency, frequency, startDate, endDate);
        var responseId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<CreatePayment>._, A<CancellationToken>._))
            .Returns(new CreatePayment.Response(responseId, userId, paymentSourceId, payeeId, amount, currency, frequency, startDate, endDate));

        // Act
        var result = await PaymentEndpoints.HandleCreate(request, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<CreatePayment.Response>>();
        var created = result as Microsoft.AspNetCore.Http.HttpResults.Created<CreatePayment.Response>;
        created.ShouldNotBeNull();
        created.Value.ShouldNotBeNull();
        created.Value!.Id.ShouldBe(responseId);
        created.Value.UserId.ShouldBe(userId);
        created.Value.PaymentSourceId.ShouldBe(paymentSourceId);
        created.Value.PayeeId.ShouldBe(payeeId);
        created.Value.Amount.ShouldBe(amount);
        created.Value.Currency.ShouldBe(currency);
        created.Value.Frequency.ShouldBe(frequency);
        created.Value.StartDate.ShouldBe(startDate);
        created.Value.EndDate.ShouldBe(endDate);
        created.Location.ShouldBe($"/api/payments/{responseId}");
    }

    [Test]
    public async Task HandleGetAll_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        var payments = new List<GetAllPayments.Response.PaymentDto>
        {
            new(Guid.NewGuid(), userId, Guid.NewGuid(), Guid.NewGuid(), 50.00m, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 1), null),
            new(Guid.NewGuid(), userId, Guid.NewGuid(), Guid.NewGuid(), 200.00m, "USD", PaymentFrequency.Annually, new DateOnly(2025, 6, 1), new DateOnly(2026, 6, 1))
        };
        A.CallTo(() => sender.Send(A<GetAllPayments>._, A<CancellationToken>._))
            .Returns(new GetAllPayments.Response(payments));

        // Act
        var result = await PaymentEndpoints.HandleGetAll(userId, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPayments.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPayments.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Payments.Count.ShouldBe(2);
    }

    [Test]
    public async Task HandleGet_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentSourceId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var amount = 75.25m;
        var currency = "GBP";
        var frequency = PaymentFrequency.Monthly;
        var startDate = new DateOnly(2025, 3, 1);
        var endDate = new DateOnly(2025, 9, 30);
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<GetPayment>._, A<CancellationToken>._))
            .Returns(new GetPayment.Response(id, userId, paymentSourceId, payeeId, amount, currency, frequency, startDate, endDate));

        // Act
        var result = await PaymentEndpoints.HandleGet(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetPayment.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetPayment.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Id.ShouldBe(id);
        ok.Value.UserId.ShouldBe(userId);
        ok.Value.PaymentSourceId.ShouldBe(paymentSourceId);
        ok.Value.PayeeId.ShouldBe(payeeId);
        ok.Value.Amount.ShouldBe(amount);
        ok.Value.Currency.ShouldBe(currency);
        ok.Value.Frequency.ShouldBe(frequency);
        ok.Value.StartDate.ShouldBe(startDate);
        ok.Value.EndDate.ShouldBe(endDate);
    }

    [Test]
    public async Task HandleUpdate_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var paymentSourceId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var amount = 150.00m;
        var currency = "EUR";
        var frequency = PaymentFrequency.Annually;
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2026, 1, 1);
        var request = new PaymentEndpoints.UpdateRequest(userId, paymentSourceId, payeeId, amount, currency, frequency, startDate, endDate);
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<UpdatePayment>._, A<CancellationToken>._))
            .Returns(new UpdatePayment.Response(id, userId, paymentSourceId, payeeId, amount, currency, frequency, startDate, endDate));

        // Act
        var result = await PaymentEndpoints.HandleUpdate(id, request, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePayment.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePayment.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Id.ShouldBe(id);
        ok.Value.UserId.ShouldBe(userId);
        ok.Value.PaymentSourceId.ShouldBe(paymentSourceId);
        ok.Value.PayeeId.ShouldBe(payeeId);
        ok.Value.Amount.ShouldBe(amount);
        ok.Value.Currency.ShouldBe(currency);
        ok.Value.Frequency.ShouldBe(frequency);
        ok.Value.StartDate.ShouldBe(startDate);
        ok.Value.EndDate.ShouldBe(endDate);
    }

    [Test]
    public async Task HandleDelete_Should_Return_NoContentResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var sender = A.Fake<ISender>();

        // Act
        var result = await PaymentEndpoints.HandleDelete(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}
