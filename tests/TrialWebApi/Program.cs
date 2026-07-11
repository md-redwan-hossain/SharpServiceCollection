using Microsoft.AspNetCore.Mvc;
using SharpServiceCollection.Generated;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationItemsAsync(1);
await builder.Services.ExecuteServiceRegistrationItemsAsync("demo");
await builder.Services.ExecuteServiceRegistrationItemsAsync(("demo", 13));
await builder.Services.ExecuteServiceRegistrationItemsAsync(("demo", decimal.MaxValue));

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();