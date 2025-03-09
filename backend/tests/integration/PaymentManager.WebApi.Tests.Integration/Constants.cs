using System;

namespace PaymentManager.WebApi.Tests.Integration;

public static class Constants
{
    public static class AppSettingsFiles
    {
        public const string IntegrationTestAppSettings = "integration-test.appsettings.json";
        public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, IntegrationTestAppSettings);
    }
}
