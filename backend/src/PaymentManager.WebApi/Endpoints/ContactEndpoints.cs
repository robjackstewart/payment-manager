using System.Net;
using System.Net.Mime;
using PaymentManager.Application.Common.Dispatch;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Endpoints;

internal static class ContactEndpoints
{
    public record CreateRequest(string Name);
    public record UpdateRequest(string Name);

    public static WebApplication Map(WebApplication app)
    {
        app.MapPost("/api/contacts", ([FromBody] CreateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleCreate(request, sender, userService, cancellationToken))
            .WithName("Create Contact")
            .Produces<CreateContact.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapGet("/api/contacts", ([FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleGetAll(sender, userService, cancellationToken))
            .WithName("Get All Contacts")
            .Produces<GetAllContacts.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

        app.MapPut("/api/contacts/{id:guid}", ([FromRoute] Guid id, [FromBody] UpdateRequest request, [FromServices] ISender sender, [FromServices] IUserService userService, CancellationToken cancellationToken) => HandleUpdate(id, request, sender, userService, cancellationToken))
            .WithName("Update Contact")
            .Produces<UpdateContact.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        app.MapDelete("/api/contacts/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => HandleDelete(id, sender, cancellationToken))
            .WithName("Delete Contact")
            .Produces((int)HttpStatusCode.NoContent);

        return app;
    }

    internal static async Task<IResult> HandleCreate(CreateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateContact(userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Created($"/api/contacts/{result.Id}", result);
    }

    internal static async Task<IResult> HandleGetAll(ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllContacts(userService.GetCurrentUserId()), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleUpdate(Guid id, UpdateRequest request, ISender sender, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateContact(id, userService.GetCurrentUserId(), request.Name), cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleDelete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteContact(id), cancellationToken);
        return Results.NoContent();
    }
}
