using CommunityToolkit.Diagnostics;
using PaymentManager.WebApi;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration.Get<Configuration>();
Guard.IsNotNull(configuration);
builder.Services.AddPaymentManagerWebApi(configuration);
var app = builder.Build();
app.ConfigurePaymentManagerWebApi();
await app.RunAsync();
