using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class GetUserEndpoint
{
    internal static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUser(id), cancellationToken);
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/api/users/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => Handle(id, sender, cancellationToken))
        .WithName("Get User")
        .Produces<GetUser.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);

        return app;
    }
}
