using NUnit.Framework;

namespace PaymentManager.WebApi.Tests.Integration;

[SetUpFixture]
public static class TestRun
{
    public static ApplicationUnderTest App { get; private set; } = null!;

    [OneTimeSetUp]
    public static async Task SetUpAsync()
    {
        App = await new ApplicationUnderTest.Builder().BuildAsync();
        await App.StartAsync();
        await App.WaitToBeHealthy();
    }

    [OneTimeTearDown]
    public static async Task TearDownAsync()
    {
        await App.DisposeAsync();
    }
}
