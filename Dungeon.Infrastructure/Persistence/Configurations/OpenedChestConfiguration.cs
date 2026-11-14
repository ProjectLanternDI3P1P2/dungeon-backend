using Dungeon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dungeon.Infrastructure.Persistence.Configurations;

public sealed class OpenedChestConfiguration : IEntityTypeConfiguration<OpenedChest>
{
    public void Configure(EntityTypeBuilder<OpenedChest> builder)
    {
        builder.ToTable("OpenedChests");

        // A generated chest can be opened once per run. The composite primary key also makes
        // retried requests and concurrent attempts unable to create duplicate state.
        builder.HasKey(chest => new
        {
            chest.DungeonRunId,
            chest.Floor,
            chest.ElementId,
        });

        builder.Property(chest => chest.Floor).IsRequired();

        builder.Property(chest => chest.ElementId).IsRequired();

        builder.Property(chest => chest.OpenedAt).IsRequired();

        builder
            .HasOne<DungeonRun>()
            .WithMany(run => run.OpenedChests)
            .HasForeignKey(chest => chest.DungeonRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
