using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace PaymentManager.Application.Common.Dispatch;

internal sealed class Dispatcher(IServiceProvider sp) : ISender
{
    private static readonly MethodInfo SendTypedMethod =
        typeof(Dispatcher).GetMethod(nameof(SendTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var method = SendTypedMethod.MakeGenericMethod(request.GetType(), typeof(TResponse));
        return (Task<TResponse>)method.Invoke(this, [request, cancellationToken])!;
    }

    private Task<TResponse> SendTyped<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToArray();
        RequestHandlerDelegate<TResponse> next = () => handler.Handle(request, cancellationToken);
        return behaviors.Aggregate(next, (n, b) => () => b.Handle(request, n, cancellationToken))();
    }
}
