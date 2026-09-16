using System.Net;
using System.Net.Mime;
using PaymentManager.Application.Common.Dispatch;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Endpoints;

internal static class PayerGroupEndpoints
{
    public record CreateRequest(string Name);
    public record UpdateRequest(string Name);
    public record SetMembersRequest(IReadOnlyList<Guid> PersonIds);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/payer-groups", ([FromBody] CreateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleCreate(request, sender, userService, cancellationToken))
            .WithName("Create Payer Group")
            .Produces<CreatePayerGroup.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/payer-groups", ([FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetAll(sender, userService, cancellationToken))
            .WithName("Get All Payer Groups")
            .Produces<GetAllPayerGroups.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapPut("/api/payer-groups/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, userService, cancellationToken))
            .WithName("Update Payer Group")
            .Produces<UpdatePayerGroup.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/payer-groups/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Payer Group")
            .Produces((int)HttpStatusCode.NoContent)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/payer-groups/{id:guid}/members", ([FromRoute] Guid id, [FromBody] SetMembersRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleSetMembers(id, request, sender, userService, cancellationToken))
            .WithName("Set Payer Group Members")
            .Produces<SetPayerGroupMembers.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        return app;
    }

    internal static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePayerGroup(userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Created($"/api/payer-groups/{result.Id}", result);
    }

    internal static async Task<IResult> HandleGetAll(ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPayerGroups(userService.GetCurrentUserId()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePayerGroup(id, userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePayerGroup(id), cancellationToken);
        return Results.NoContent();
    }

    internal static async Task<IResult> HandleSetMembers(Guid id, SetMembersRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetPayerGroupMembers(id, userService.GetCurrentUserId(), request.PersonIds), cancellationToken);
        return Results.Ok(result);
    }
}
