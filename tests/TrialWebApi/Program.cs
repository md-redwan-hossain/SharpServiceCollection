using Microsoft.AspNetCore.Mvc;
using SharpServiceCollection.Generated;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddServiceRegistrationItemsAsync();
await builder.Services.AddServiceRegistrationItemsAsync(TimeOnly.FromDateTime(DateTime.Now));
await builder.Services.AddServiceRegistrationItemsAsync("demo");
await builder.Services.AddServiceRegistrationItemsAsync(("demo", 13));
await builder.Services.AddServiceRegistrationItemsAsync(("demo", decimal.MaxValue));

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();