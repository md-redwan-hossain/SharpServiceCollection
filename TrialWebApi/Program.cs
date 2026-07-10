using Microsoft.AspNetCore.Mvc;
using SharpServiceCollection.Extensions;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddAttributedServices();
builder.Services.AddServicesFromCurrentAssembly();

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();