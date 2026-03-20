using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Enums;

namespace PaymentManager.WebApi.Endpoints;

internal static class PaymentEndpoints
{
    public record CreateRequest(Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate);
    public record UpdateRequest(Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payments", ([FromBody] CreateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleCreate(request, sender, cancellationToken))
            .WithName("Create Payment")
            .Produces<CreatePayment.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payments", ([FromQuery] Guid userId, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGetAll(userId, sender, cancellationToken))
            .WithName("Get All Payments")
            .Produces<GetAllPayments.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get Payment")
            .Produces<GetPayment.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, cancellationToken))
            .WithName("Update Payment")
            .Produces<UpdatePayment.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payments/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payment")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    private static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePayment(request.UserId, request.PaymentSourceId, request.PayeeId, request.Amount, request.Frequency, request.StartDate, request.EndDate), cancellationToken);
        return Results.Created($"/api/payments/{result.Id}", result);
    }

    private static async Task<IResult> HandleGetAll(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPayments(userId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPayment(id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePayment(id, request.UserId, request.PaymentSourceId, request.PayeeId, request.Amount, request.Frequency, request.StartDate, request.EndDate), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePayment(id), cancellationToken);
        return Results.NoContent();
    }
}
