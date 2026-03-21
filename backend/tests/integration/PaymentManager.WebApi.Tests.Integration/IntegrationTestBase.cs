using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi.Tests.Integration;

[TestFixture]
public abstract class IntegrationTestBase
{
    private IServiceScope _scope = null!;

    protected ApplicationUnderTest App => TestRun.App;

    protected T GetService<T>() where T : notnull =>
        _scope.ServiceProvider.GetRequiredService<T>();

    protected HttpClient CreateApiClient() => App.CreateApiClient();

    [SetUp]
    public async Task SetUpAsync()
    {
        _scope = App.Services.CreateScope();

        var context = _scope.ServiceProvider.GetRequiredService<IPaymentManagerContext>();
        await context.Payments.ExecuteDeleteAsync();
        await context.Payees.ExecuteDeleteAsync();
        await context.PaymentSources.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();

        // Re-insert the default user so the API can associate new resources with it
        context.Users.Add(new User { Id = DefaultUserService.DefaultUserId, Name = "Default User" });
        await context.SaveChanges(CancellationToken.None);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();
}
