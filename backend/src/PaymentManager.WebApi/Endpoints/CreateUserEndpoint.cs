using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;

namespace PaymentManager.WebApi.Endpoints;

internal static class CreateUserEndpoint
{
    public static async Task<IResult> Handle(CreateUser request, ISender sender)
    {
        var result = await sender.Send(request);
        return Results.Created($"/api/users/{result.Id}", result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapPost("/api/user", ([FromBody] CreateUser request, [FromServices] ISender sender) => Handle(request, sender))
        .WithName("Create User")
        .Produces<CreateUser.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);
        return app;
    }
}
