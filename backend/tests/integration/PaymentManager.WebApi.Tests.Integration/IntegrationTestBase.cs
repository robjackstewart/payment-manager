using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
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
        await context.PaymentSplits.ExecuteDeleteAsync();
        await context.Payments.ExecuteDeleteAsync();
        await context.Contacts.ExecuteDeleteAsync();
        await context.Payees.ExecuteDeleteAsync();
        await context.PaymentSources.ExecuteDeleteAsync();
        await context.Users.Where(u => u.Id != DefaultUserService.DefaultUserId).ExecuteDeleteAsync();
        await context.SaveChanges(CancellationToken.None);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();
}
