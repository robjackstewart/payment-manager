using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentManager.Application.Common;

namespace PaymentManager.WebApi.Tests.Integration;

internal class PaymentMangerWebApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.Sources.Clear();
            config.AddJsonFile(Constants.AppSettingsFiles.FilePath);
        });
        var host = base.CreateHost(builder);

        var context = host.Services.GetRequiredService<IPaymentManagerContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        return host;
    }
}
