using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

public class MobRulesTests
{
    [Theory]
    [InlineData("110", new[] { true, true, false, true, true, false })]
    [InlineData("1110", new[] { true, true, true, false, true, true })]
    [InlineData("001", new[] { false, false, true, false, false, true })]
    [InlineData("1", new[] { true, true, true, true, true, true })]
    public void ActsOn_FollowsTheCadencePattern(string cadence, bool[] expected)
    {
        // Arrange
        MobBehaviour behaviour = new(cadence, 1, MobStep.Orthogonal, MobIdle.None);

        // Act
        bool[] acts = [.. Enumerable.Range(0, expected.Length).Select(behaviour.ActsOn)];

        // Assert
        acts.Should().Equal(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("000")]
    [InlineData("12")]
    public void MobBehaviour_InvalidCadence_Throws(string cadence)
    {
        // Act
        Action act = () => _ = new MobBehaviour(cadence, 1, MobStep.Orthogonal, MobIdle.None);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MobBehaviour_TeleportWithoutDistance_Throws()
    {
        // Act
        Action act = () => _ = new MobBehaviour("001", null, MobStep.Teleport, MobIdle.None);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MobRules_MissingAKind_Throws()
    {
        // Act
        Action act = () =>
            _ = new MobRules(
                2,
                new Dictionary<EnemyKind, MobBehaviour>
                {
                    [EnemyKind.Zombie] = MobRules.Default.For(EnemyKind.Zombie),
                }
            );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Default_MatchesTheSpec()
    {
        // Act
        MobRules rules = MobRules.Default;

        // Assert
        rules.WanderRadius.Should().Be(2);
        rules
            .For(EnemyKind.Zombie)
            .Should()
            .Be(new MobBehaviour("110", 10, MobStep.Orthogonal, MobIdle.Wander));
        rules
            .For(EnemyKind.Bat)
            .Should()
            .Be(new MobBehaviour("1110", 5, MobStep.Diagonal, MobIdle.Wander));
        rules
            .For(EnemyKind.Goblin)
            .Should()
            .Be(new MobBehaviour("1", 3, MobStep.Orthogonal, MobIdle.NearestDoor));
        rules
            .For(EnemyKind.Mage)
            .Should()
            .Be(new MobBehaviour("001", null, MobStep.Teleport, MobIdle.None, 3));
    }
}
