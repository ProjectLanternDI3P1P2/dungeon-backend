using Dungeon.Application;
using Dungeon.Infrastructure;
using Dungeon.Infrastructure.Persistence.Seeding;
using Dungeon.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApi();

builder
    .Services.AddInfrastructureServices(builder.Configuration, builder.Environment.IsDevelopment())
    .AddApplicationServices();

var app = builder.Build();

// TODO(tech-debt): migrations only run in Development, nothing applies them in k3s
// (ADR-0036) — add a migration job or a --migrate command to the image.
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();
