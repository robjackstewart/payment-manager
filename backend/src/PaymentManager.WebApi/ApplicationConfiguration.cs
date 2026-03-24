using PaymentManager.WebApi.Endpoints;

namespace PaymentManager.WebApi;

public static class ApplicationConfiguration
{
    public static WebApplication ConfigurePaymentManagerWebApi(this WebApplication app, Configuration configuration)
    {
        if (!string.IsNullOrEmpty(configuration.BasePath))
        {
            app.UsePathBase(configuration.BasePath);
            app.UseRouting();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/openapi/v1.json", "Payment Manager");
            });
        }

        app.UseCors(Constants.Cors.ALLOW_ALL_POLICY_NAME);

        app.UseExceptionHandler(_ => { });
        app.MapEndpoints();
        app.MapFallbackToFile("index.html");
        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        UserEndpoints.Map(app);
        PaymentSourceEndpoints.Map(app);
        PayeeEndpoints.Map(app);
        ContactEndpoints.Map(app);
        PaymentEndpoints.Map(app);
        return app;
    }
}
