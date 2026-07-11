using Microsoft.AspNetCore.Mvc;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationItemsAsync();
await builder.Services.ExecuteServiceRegistrationItemsAsync("demo");

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();