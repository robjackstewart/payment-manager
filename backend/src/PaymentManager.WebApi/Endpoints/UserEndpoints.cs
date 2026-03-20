using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class UserEndpoints
{
    public record CreateRequest(string Name);
    public record UpdateRequest(string Name);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/users", ([FromBody] CreateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleCreate(request, sender, cancellationToken))
            .WithName("Create User")
            .Produces<CreateUser.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/users", ([FromServices] ISender sender, CancellationToken cancellationToken) => HandleGetAll(sender, cancellationToken))
            .WithName("Get All Users")
            .Produces<GetAllUsers.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapGet("/api/users/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleGet(id, sender, cancellationToken))
            .WithName("Get User")
            .Produces<GetUser.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapPut("/api/users/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, cancellationToken))
            .WithName("Update User")
            .Produces<UpdateUser.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/users/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete User")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    private static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUser(request.Name), cancellationToken);
        return Results.Created($"/api/users/{result.Id}", result);
    }

    private static async Task<IResult> HandleGetAll(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllUsers(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGet(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUser(id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUser(id, request.Name), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUser(id), cancellationToken);
        return Results.NoContent();
    }
}
