using System;
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetUser(id));
        return Results.Ok(result);
    }

    public static WebApplication MapGetUserEndpoint(this WebApplication app)
    {
        app.MapGet("/api/users/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender) => Handle(id, sender))
        .WithName("Get User")
        .Produces<GetUser.Response>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.BadRequest); ;

        return app;
    }
}
