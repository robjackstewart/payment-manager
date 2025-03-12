using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

public static class GetAllPaymentSourcesEndpoint
{
    internal static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllPaymentSources(), cancellationToken);
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/api/paymentsources",
            ([FromServices] ISender sender, CancellationToken cancellationToken) => Handle(sender, cancellationToken))
            .WithName("Get all payment sources")
            .Produces<GetAllPaymentSources.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);
        return app;
    }
}
