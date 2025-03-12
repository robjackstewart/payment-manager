using PaymentManager.WebApi.Endpoints;

namespace PaymentManager.WebApi;

public static class ApplicationConfiguration
{
    public static WebApplication ConfigurePaymentManagerWebApi(this WebApplication app)
    {
        app.MapEndpoints();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/openapi/v1.json", "Payment Manager");
            });
        }
        app.UseCors(Constants.Cors.ALLOW_UI_POLICY_NAME);
        app.UseExceptionHandler(_ => { });
        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        GetUserEndpoint.Map(app);
        CreateUserEndpoint.Map(app);
        GetAllUsersEndpoint.Map(app);
        GetPaymentOccurencesEndpoint.Map(app);
        GetPaymentSourceEndpoint.Map(app);
        GetAllPaymentSourcesEndpoint.Map(app);
        CreatePaymentSourceEndpoint.Map(app);
        return app;
    }
}
