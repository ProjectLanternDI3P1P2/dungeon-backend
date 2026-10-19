using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Dungeon.Test.Infrastructure.Services;

public class MobOptionsTests
{
    private static MobOptions Bind(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection(MobOptions.SectionName)
            .Get<MobOptions>()
        ?? new MobOptions();

    [Fact]
    public void ToRules_NoConfiguration_GivesTheDefaultRules()
    {
        // Act
        MobRules rules = Bind([]).ToRules();

        // Assert
        rules.WanderRadius.Should().Be(MobRules.Default.WanderRadius);
        foreach (EnemyKind kind in Enum.GetValues<EnemyKind>())
        {
            rules.For(kind).Should().Be(MobRules.Default.For(kind));
        }
    }

    [Fact]
    public void ToRules_OneValueOverridden_KeepsTheOtherDefaults()
    {
        // Act
        MobRules rules = Bind(new() { ["Dungeon:Mobs:Zombie:Cadence"] = "1" }).ToRules();

        // Assert
        rules.For(EnemyKind.Zombie).Cadence.Should().Be("1");
        rules.For(EnemyKind.Zombie).AggroRange.Should().Be(10);
        rules.For(EnemyKind.Mage).AggroRange.Should().BeNull();
    }

    [Fact]
    public void ToRules_InvalidCadence_Throws()
    {
        // Act
        Action act = () => Bind(new() { ["Dungeon:Mobs:Bat:Cadence"] = "000" }).ToRules();

        // Assert: the service refuses to start rather than freezing every bat.
        act.Should().Throw<ArgumentException>();
    }
}
