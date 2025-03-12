using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

public static class GetAllUsersEndpoint
{
    public static async Task<IResult> Handle(ISender sender)
    {
        var result = await sender.Send(new GetAllUsers());
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/api/users", ([FromServices] ISender sender) => Handle(sender))
        .WithName("Get all users")
        .Produces<GetAllUsers.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);
        return app;
    }
}
