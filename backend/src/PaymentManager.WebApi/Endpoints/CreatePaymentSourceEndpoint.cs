using System;
using System.Net;
using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Commands;
using PaymentManager.Domain.Entities;

namespace PaymentManager.WebApi.Endpoints;

internal static class CreatePaymentSourceEndpoint
{
    public record Request(string Name, string? Description);

    internal static async Task<IResult> Handle(Request request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePaymentSource(User.DefaultUser.Id, request.Name, request.Description), cancellationToken);
        return Results.Created($"/api/paymentsource/{result.Id}", result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapPost("/api/payments/source", ([FromBody] Request request, [FromServices] ISender sender, CancellationToken cancellationToken) => Handle(request, sender, cancellationToken))
        .WithName("Create payment source")
        .Accepts<Request>(MediaTypeNames.Application.Json)
        .Produces<CreatePaymentSource.Response>((int)HttpStatusCode.Created, MediaTypeNames.Application.Json)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest, MediaTypeNames.Application.Json);
        return app;
    }
}
