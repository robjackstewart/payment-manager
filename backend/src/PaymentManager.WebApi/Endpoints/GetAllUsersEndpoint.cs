using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class GetAllUsersEndpoint
{
    internal static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllUsers(), cancellationToken);
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/api/users", ([FromServices] ISender sender, CancellationToken cancellationToken) => Handle(sender, cancellationToken))
        .WithName("Get all users")
        .Produces<GetAllUsers.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);
        return app;
    }
}
