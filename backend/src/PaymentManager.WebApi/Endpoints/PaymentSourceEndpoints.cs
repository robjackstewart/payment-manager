using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class PaymentSourceEndpoints
{
    public record CreateRequest(Guid UserId, string Name);
    public record UpdateRequest(Guid UserId, string Name);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payment-sources", ([FromBody] CreateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleCreate(request, sender, cancellationToken))
            .WithName("Create Payment Source")
            .Produces<CreatePaymentSource.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payment-sources", ([FromQuery] Guid userId, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGetAll(userId, sender, cancellationToken))
            .WithName("Get All Payment Sources")
            .Produces<GetAllPaymentSources.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get Payment Source")
            .Produces<GetPaymentSource.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, cancellationToken))
            .WithName("Update Payment Source")
            .Produces<UpdatePaymentSource.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payment Source")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    private static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePaymentSource(request.UserId, request.Name), cancellationToken);
        return Results.Created($"/api/payment-sources/{result.Id}", result);
    }

    private static async Task<IResult> HandleGetAll(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPaymentSources(userId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentSource(id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePaymentSource(id, request.UserId, request.Name), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePaymentSource(id), cancellationToken);
        return Results.NoContent();
    }
}
