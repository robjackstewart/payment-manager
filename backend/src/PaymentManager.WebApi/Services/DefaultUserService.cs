namespace PaymentManager.WebApi.Services;

public sealed class DefaultUserService : IUserService
{
    public static readonly Guid DefaultUserId = new("11111111-1111-1111-1111-111111111111");

    public Guid GetCurrentUserId() => DefaultUserId;
}
