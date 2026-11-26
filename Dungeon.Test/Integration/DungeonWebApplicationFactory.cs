using Dungeon.Application.Ports;
using Dungeon.Infrastructure.Persistence;
using Dungeon.Test.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dungeon.Test.Integration;

public sealed class DungeonWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["RabbitMq:Enabled"] = "false",
                }
            )
        );
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DungeonDbContext>>();
            services.RemoveAll<DungeonDbContext>();
            services.RemoveAll<IOptions<DatabaseOptions>>();
            services.AddSingleton<IOptions<DatabaseOptions>>(
                Options.Create(new DatabaseOptions { DefaultConnection = connectionString })
            );
            services.AddDbContext<DungeonDbContext>(options => options.UseNpgsql(connectionString));

            // Player is not running: the tests choose what it would answer.
            services.RemoveAll<IHeroAppearanceClient>();
            services.AddSingleton<FakeHeroAppearanceClient>();
            services.AddSingleton<IHeroAppearanceClient>(provider =>
                provider.GetRequiredService<FakeHeroAppearanceClient>()
            );
        });
    }
}
