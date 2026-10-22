using BuildingBlocks.Web;
using BuildingBlocks.Web.Endpoints;
using ServiceName.Application;
using ServiceName.Infrastructure;
using ServiceName.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddServiceNameApplication()
    .AddServiceNameInfrastructure(builder.Configuration)
    .AddServiceNamePersistence(builder.Configuration)
    .AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.UseServiceDefaults();
app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api/v1"));

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host it with WebApplicationFactory.</summary>
public partial class Program;
