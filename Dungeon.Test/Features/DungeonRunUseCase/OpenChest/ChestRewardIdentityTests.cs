using Dungeon.Application.Features.DungeonRunUseCase.OpenChest;
using FluentAssertions;

namespace Dungeon.Test.Features.DungeonRunUseCase.OpenChest;

public sealed class ChestRewardIdentityTests
{
    [Fact]
    public void For_SameContextAsReward_ReturnsTheSameStableVersion5Uuid()
    {
        var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Guid result = ChestRewardIdentity.For(runId, floor: 1, elementId: 0);

        result.Should().Be(Guid.Parse("c24d1caf-1a51-59b6-9f4d-d51bfafaa1e9"));
        result.Version.Should().Be(5);
    }

    [Fact]
    public void For_DifferentContextParts_ReturnsIndependentIds()
    {
        Guid firstRun = Guid.NewGuid();
        Guid secondRun = Guid.NewGuid();

        Guid baseline = ChestRewardIdentity.For(firstRun, floor: 1, elementId: 0);
        Guid otherRun = ChestRewardIdentity.For(secondRun, floor: 1, elementId: 0);
        Guid otherFloor = ChestRewardIdentity.For(firstRun, floor: 2, elementId: 0);
        Guid otherElement = ChestRewardIdentity.For(firstRun, floor: 1, elementId: 1);

        new[] { baseline, otherRun, otherFloor, otherElement }.Should().OnlyHaveUniqueItems();
    }
}
