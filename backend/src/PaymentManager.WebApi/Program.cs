using PaymentManager.WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPaymentManagerWebApi(builder.Configuration);
var app = builder.Build();
app.ConfigurePaymentManagerWebApi();
await app.RunAsync();
