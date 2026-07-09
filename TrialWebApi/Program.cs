using SharpServiceCollection.Generated;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddGeneratedServices();

var app = builder.Build();

app.MapGet("hello", () => "Hello World!");

app.Run();