using System.Net;
using System.Net.Mime;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Application.Queries;

namespace PaymentManager.WebApi.Endpoints;

internal static class GetPaymentsEndpoint
{
    public record Request(string? From, string? To)
    {
        internal class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.From).Must(f => DateOnly.TryParse(f, out _)).When(x => x.From is not null);
                RuleFor(x => x.To).Must(t => DateOnly.TryParse(t, out _)).When(x => x.To is not null);
            }
        }
    };

    public static async Task<IResult> Handle(Request request, ISender sender, CancellationToken cancellationToken)
    {
        await ValidationHandler<Request>.ThrowIfInvalid(new Request.Validator(), request, cancellationToken);
        var from = request.From is not null ? DateOnly.Parse(request.From) : DateOnly.MinValue;
        var to = request.To is not null ? DateOnly.Parse(request.To) : DateOnly.MaxValue;
        var result = await sender.Send(new GetPayments(from, to), cancellationToken);
        return Results.Ok(result);
    }

    public static WebApplication Map(this WebApplication app)
    {
        app.MapGet("/payments", ([FromQuery(Name = nameof(from))] string? from, [FromQuery(Name = nameof(to))] string? to,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) => Handle(new Request(from, to), sender, cancellationToken))
                    .Produces<GetPayments.Response>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json)
                    .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);
        return app;
    }
}
