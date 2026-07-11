using Microsoft.AspNetCore.Mvc;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationsAsync();
await builder.Services.ExecuteServiceRegistrationsAsync("demo");
await builder.Services.ExecuteServiceRegistrationsAsync(1);

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();