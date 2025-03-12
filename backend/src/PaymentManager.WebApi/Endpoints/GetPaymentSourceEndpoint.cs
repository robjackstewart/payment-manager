using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class GetPaymentSourceEndpoint
{
    internal static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentSource(id), cancellationToken);
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/api/paymentsource/{id:guid}", ([FromRoute] Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) => Handle(id, sender, cancellationToken))
        .WithName("Get payment source")
        .Produces<GetPaymentSource.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);
        return app;
    }
}
