using System;
using System.Net;
using System.Net.Mime;
using PaymentManager.Application.Common.Dispatch;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Endpoints;

internal static class PayeeEndpoints
{
    public record CreateRequest(string Name);
    public record UpdateRequest(string Name);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payees", ([FromBody] CreateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleCreate(request, sender, userService, cancellationToken))
            .WithName("Create Payee")
            .Produces<CreatePayee.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payees", ([FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetAll(sender, userService, cancellationToken))
            .WithName("Get All Payees")
            .Produces<GetAllPayees.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/payees/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get Payee")
            .Produces<GetPayee.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payees/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, userService, cancellationToken))
            .WithName("Update Payee")
            .Produces<UpdatePayee.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payees/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payee")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    internal static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePayee(userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Created($"/api/payees/{result.Id}", result);
    }

    internal static async Task<IResult> HandleGetAll(ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPayees(userService.GetCurrentUserId()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPayee(id), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePayee(id, userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePayee(id), cancellationToken);
        return Results.NoContent();
    }
}
