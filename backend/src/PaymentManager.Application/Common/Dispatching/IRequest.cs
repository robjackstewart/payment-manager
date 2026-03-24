namespace PaymentManager.Application.Common.Dispatch;

public interface IRequest<TResponse> { }

public interface IRequest : IRequest<Unit> { }
