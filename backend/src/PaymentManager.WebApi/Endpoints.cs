using System;
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi;

public static class Endpoints
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapUserEndpoints();
        return app;
    }

    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        app.MapPost("/api/User", async ([FromBody] CreateUser request, [FromServices] ISender sender) =>
        {
            var result = await sender.Send(request);
            return Results.Created($"/api/User/{result.Id}", result);
        })
        .WithName("Create User")
        .Produces<CreateUser.Response>((int)HttpStatusCode.Created)
        .Produces((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/User/{id:guid}", async ([FromRoute] Guid id, [FromServices] ISender sender) =>
        {
            var result = await sender.Send(new GetUser(id));
            return Results.Ok(result);
        })
        .WithName("Get User")
        .Produces<GetUser.Response>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.BadRequest); ;

        return app;
    }
}
