using System;
using System.Net;
using System.Net.Mime;
using PaymentManager.Application.Common.Dispatch;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Endpoints;

internal static class PaymentEndpoints
{
    public record CreateRequest(Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description = null, IReadOnlyList<SplitRequest>? Splits = null);
    public record UpdateRequest(Guid PaymentSourceId, Guid PayeeId, decimal InitialAmount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description = null, IReadOnlyList<SplitRequest>? Splits = null);
    public record SplitRequest(Guid ContactId, decimal Percentage);
    public record EffectiveValueRequest(DateOnly EffectiveDate, decimal Amount);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payments", ([FromBody] CreateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleCreate(request, sender, userService, cancellationToken))
            .WithName("Create Payment")
            .Produces<CreatePayment.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payments", ([FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetAll(sender, userService, cancellationToken))
            .WithName("Get All Payments")
            .Produces<GetAllPayments.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payments/occurrences", ([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetOccurrences(from, to, sender, userService, cancellationToken))
            .WithName("Get Payment Occurrences")
            .Produces<GetPaymentOccurrences.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get Payment")
            .Produces<GetPayment.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, userService, cancellationToken))
            .WithName("Update Payment")
            .Produces<UpdatePayment.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payment")
            .Produces((int)HttpStatusCode.NoContent);

        app.MapPost("/api/payments/{id:guid}/values", ([FromRoute] Guid id, [FromBody] EffectiveValueRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleAddValue(id, request, sender, cancellationToken))
            .WithName("Add Payment Value")
            .Produces<AddPaymentValue.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payments/{id:guid}/values/{effectiveDate}", ([FromRoute] Guid id, [FromRoute] DateOnly effectiveDate, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleRemoveValue(id, effectiveDate, sender, cancellationToken))
            .WithName("Remove Payment Value")
            .Produces((int)HttpStatusCode.NoContent)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound, MediaTypeNames.Application.Json);

        return app;
    }

    internal static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePayment(userService.GetCurrentUserId(), request.PaymentSourceId, request.PayeeId, request.Amount, request.Currency, request.Frequency, request.StartDate, request.EndDate, request.Description, request.Splits?.Select(s => new CreatePayment.SplitRequest(s.ContactId, s.Percentage)).ToList()), cancellationToken);
        return Results.Created($"/api/payments/{result.Id}", result);
    }

    internal static async Task<IResult> HandleGetAll(ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPayments(userService.GetCurrentUserId()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleGetOccurrences(DateOnly from, DateOnly to, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentOccurrences(userService.GetCurrentUserId(), from, to), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPayment(id), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePayment(id, userService.GetCurrentUserId(), request.PaymentSourceId, request.PayeeId, request.InitialAmount, request.Currency, request.Frequency, request.StartDate, request.EndDate, request.Description, request.Splits?.Select(s => new UpdatePayment.SplitRequest(s.ContactId, s.Percentage)).ToList()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePayment(id), cancellationToken);
        return Results.NoContent();
    }

    internal static async Task<IResult> HandleAddValue(Guid id, EffectiveValueRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddPaymentValue(id, request.EffectiveDate, request.Amount), cancellationToken);
        return Results.Created($"/api/payments/{id}/values", result);
    }

    internal static async Task<IResult> HandleRemoveValue(Guid id, DateOnly effectiveDate, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new RemovePaymentValue(id, effectiveDate), cancellationToken);
        return Results.NoContent();
    }
}
