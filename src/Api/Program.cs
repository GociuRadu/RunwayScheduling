using Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();
Api.Endpoints.MapAll(app);
app.ApplyDatabaseMigrations();

app.Run();
