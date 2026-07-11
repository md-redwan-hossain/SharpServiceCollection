using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using SharpServiceCollection.Generated;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationItemsAsync(1);
await builder.Services.ExecuteServiceRegistrationItemsAsync("demo");

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();