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
        app.UseExceptionHandler(_ => { });
        return app;
    }
}
