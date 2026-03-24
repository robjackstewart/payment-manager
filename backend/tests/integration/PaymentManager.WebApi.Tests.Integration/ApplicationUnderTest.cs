using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Host.Local.Infrastructure;
using PaymentManager.WebApi;

namespace PaymentManager.WebApi.Tests.Integration;

/// <summary>
/// Encapsulates the full test environment: an Aspire distributed application managing the SQLite
/// resource, and an in-process <see cref="WebApplicationFactory{TEntryPoint}"/> for the WebApi.
/// Call <see cref="StartAsync"/> after building to start both the distributed application and
/// the in-process WebApi factory.
/// </summary>
public sealed class ApplicationUnderTest : IAsyncDisposable
{
    private readonly DistributedApplication _distributedApp;
    private WebApplicationFactory<TestEntry>? _factory;
    private IServiceScope? _scope;

    private ApplicationUnderTest(DistributedApplication distributedApp)
    {
        _distributedApp = distributedApp;
    }

    public HttpClient CreateApiClient() => _factory!.CreateClient();

    public IServiceProvider Services => _factory!.Services;

    /// <summary>
    /// Resolves a service from the long-lived scope created when the factory started.
    /// Use this for scoped services (e.g. <see cref="IPaymentManagerContext"/>).
    /// </summary>
    public T GetApiService<T>() where T : notnull =>
        _scope!.ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Starts the Aspire distributed application, resolves the SQLite connection string,
    /// creates the <see cref="WebApplicationFactory{TEntryPoint}"/>, and applies migrations.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _distributedApp.StartAsync(cancellationToken);

        var connectionString = await _distributedApp.GetConnectionStringAsync("database", cancellationToken);

        _factory = new PaymentManagerWebApplicationFactory(connectionString!);
        _scope = _factory.Services.CreateScope();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IPaymentManagerContext>();
        await context.Database.EnsureDeletedAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for the SQLite database resource to be running and healthy.
    /// </summary>
    public async Task WaitForDatabaseToBeHealthy(CancellationToken cancellationToken = default)
    {
        var rns = _distributedApp.Services
            .GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync("database", KnownResourceStates.Running, cancellationToken);
    }

    /// <summary>
    /// Waits for all application resources to be healthy.
    /// </summary>
    public async Task WaitToBeHealthy(CancellationToken cancellationToken = default)
    {
        await WaitForDatabaseToBeHealthy(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _scope?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await _distributedApp.DisposeAsync();
    }

    // Internal WAF that overrides configuration with the Aspire-provided connection string
    private sealed class PaymentManagerWebApplicationFactory(string connectionString) : WebApplicationFactory<TestEntry>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PaymentManager"] = connectionString,
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                });
            });

            return base.CreateHost(builder);
        }
    }

    public sealed class Builder
    {
        /// <summary>
        /// Builds the Aspire distributed application. Call <see cref="ApplicationUnderTest.StartAsync"/>
        /// on the returned instance to start the app and create the WebApi factory.
        /// </summary>
        public async Task<ApplicationUnderTest> BuildAsync(CancellationToken cancellationToken = default)
        {
            var appHostBuilder = await DistributedApplicationTestingBuilder
                .CreateAsync<InfrastructureAppHost>(cancellationToken);

            var distributedApp = await appHostBuilder.BuildAsync(cancellationToken);

            return new ApplicationUnderTest(distributedApp);
        }
    }
}
