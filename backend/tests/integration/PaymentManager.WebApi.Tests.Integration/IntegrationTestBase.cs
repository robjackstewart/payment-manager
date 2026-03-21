using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;

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
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();
}
