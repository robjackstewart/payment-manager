using CommunityToolkit.Diagnostics;
using PaymentManager.Infrastructure;
using PaymentManager.WebApi;
using WebApiConfiguration = PaymentManager.WebApi.Configuration;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration.Get<WebApiConfiguration>();
Guard.IsNotNull(configuration);
builder.Services.AddPaymentManagerWebApi(configuration);
var app = builder.Build();
app.ConfigurePaymentManagerWebApi();
await app.RunAsync();