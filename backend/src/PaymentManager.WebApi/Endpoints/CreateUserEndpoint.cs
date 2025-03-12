using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;

namespace PaymentManager.WebApi.Endpoints;

internal static class CreateUserEndpoint
{
    public record Request(string Name);

    internal static async Task<IResult> Handle(Request request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUser(request.Name), cancellationToken);
        return Results.Created($"/api/users/{result.Id}", result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapPost("/api/user", ([FromBody] Request request, [FromServices] ISender sender, CancellationToken cancellationToken) => Handle(request, sender, cancellationToken))
        .WithName("Create User")
        .Produces<CreateUser.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);
        return app;
    }
}
