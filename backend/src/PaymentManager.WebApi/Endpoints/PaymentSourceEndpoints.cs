using System;
using System.Net;
using System.Net.Mime;
using PaymentManager.Application.Common.Dispatch;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Endpoints;

internal static class PaymentSourceEndpoints
{
    public record CreateRequest(string Name);
    public record UpdateRequest(string Name);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payment-sources", ([FromBody] CreateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleCreate(request, sender, userService, cancellationToken))
            .WithName("Create Payment Source")
            .Produces<CreatePaymentSource.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payment-sources", ([FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetAll(sender, userService, cancellationToken))
            .WithName("Get All Payment Sources")
            .Produces<GetAllPaymentSources.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get Payment Source")
            .Produces<GetPaymentSource.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, userService, cancellationToken))
            .WithName("Update Payment Source")
            .Produces<UpdatePaymentSource.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payment-sources/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payment Source")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    internal static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePaymentSource(userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Created($"/api/payment-sources/{result.Id}", result);
    }

    internal static async Task<IResult> HandleGetAll(ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPaymentSources(userService.GetCurrentUserId()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentSource(id), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePaymentSource(id, userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePaymentSource(id), cancellationToken);
        return Results.NoContent();
    }
}
