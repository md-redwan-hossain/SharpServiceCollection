using Microsoft.AspNetCore.Mvc;
using SharpServiceCollection.Generated;
using TrialWebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGeneratedServices();

var app = builder.Build();

app.MapGet("/", ([FromServices] ITrialService service) => service.HelloWorld());

app.Run();