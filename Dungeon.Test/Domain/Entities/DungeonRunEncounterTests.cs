using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Entities;

/// <summary>A contact opens one fight in Combat with the whole pack (Combat mode).</summary>
public sealed class DungeonRunEncounterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid HeroId = Guid.Parse("00000000-0000-0000-0000-00000000aaaa");
    private static readonly EncounterRules Combat = new(EncounterMode.Combat, 1, 6);

    private static (DungeonRun Run, GeneratedDungeon Dungeon) Room(
        bool withHero,
        params string[] inside
    )
    {
        string wall = new('#', inside[0].Length + 2);
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            [wall, .. inside.Select(row => $"#{row}#"), wall],
            new RoomBounds(1, 1, inside[0].Length, inside.Length)
        );
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            Now,
            withHero ? [HeroId] : null
        );
        return (run, dungeon);
    }

    private static (DungeonRun Run, GeneratedDungeon Dungeon) Room(params string[] inside) =>
        Room(true, inside);

    [Fact]
    public void Contact_OneMob_OpensAnEncounterWithThatMobAndKillsNobody()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H.z");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        RunEncounter encounter = run.OpenEncounter!;
        encounter.Status.Should().Be(EncounterStatus.Pending);
        encounter.Trigger.Should().Be(EncounterTrigger.Collision);
        encounter.MobElementIds.Should().ContainSingle().Which.Should().Be(run.Mobs[0].ElementId);
        run.Mobs[0].IsActive.Should().BeTrue();
        run.Mobs[0].InCombat.Should().BeTrue();
    }

    [Fact]
    public void Contact_SeveralMobsInReach_OpensOneEncounterWithTheWholePack()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H...", ".zz.", "....");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        run.Encounters.Should().ContainSingle();
        run.OpenEncounter!.MobElementIds.Should().HaveCount(2);
        run.Mobs.Should().OnlyContain(mob => mob.IsActive && mob.InCombat);
    }

    [Fact]
    public void Contact_MoreMobsThanThePackSize_KeepsTheNearestAndLeavesTheOthersActive()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("zzz..", "H.z..", "zzz..");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        run.OpenEncounter!.MobElementIds.Should().HaveCount(6);
        run.Mobs.Count(mob => mob.InCombat).Should().Be(6);
        run.Mobs.Should().OnlyContain(mob => mob.IsActive);
    }

    [Fact]
    public void MobTwoTilesAway_DoesNotJoin()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H.....z");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        run.OpenEncounter.Should().BeNull();
        run.Mobs[0].InCombat.Should().BeFalse();
    }

    [Fact]
    public void StepIntoAMob_IsAContact_TheHeroStaysAndTheTurnIsSpent()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");

        Position position = run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        position.Should().Be(new Position(1, 1));
        run.HeroPosition.Should().Be(new Position(1, 1));
        run.Turn.Should().Be(1);
        run.OpenEncounter!.MobElementIds.Should().ContainSingle();
    }

    [Fact]
    public void MoveHero_DuringAnEncounter_IsRefused()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        Action act = () => run.MoveHero(Direction.South, dungeon, MobRules.Default, Combat);

        act.Should().Throw<EncounterInProgressException>();
    }

    [Fact]
    public void RunWithoutHero_KeepsTheStubEvenInCombatMode()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room(false, "H.z");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        run.OpenEncounter.Should().BeNull();
        run.Mobs[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public void StubMode_ContactStillKillsTheMob()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H.z");

        run.MoveHero(Direction.East, dungeon, MobRules.Default, EncounterRules.Stub);

        run.OpenEncounter.Should().BeNull();
        run.Mobs[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public void Resolve_Won_KillsThePackOnThisTurnAndShowsTheOutcomeOnce()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H...", ".zz.", "....");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        RunEncounter encounter = run.OpenEncounter!;
        Guid combatId = Guid.NewGuid();
        run.MarkEncounterCombatStarted(encounter.Id, combatId);

        run.ResolveEncounter(
            encounter.Id,
            combatId,
            EncounterOutcome.Won,
            [new HeroFinalState(HeroId, 90, 12, false)],
            [.. encounter.MobElementIds],
            dungeon
        );

        run.Mobs.Should().OnlyContain(mob => !mob.IsActive && mob.KilledOnTurn == run.Turn);
        run.OpenEncounter.Should().BeNull();
        run.VisibleEncounter!.Outcome.Should().Be(EncounterOutcome.Won);
        run.Participants.Single().CurrentHp.Should().Be(90);
        run.Participants.Single().CurrentMana.Should().Be(12);

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        run.VisibleEncounter.Should().BeNull();
    }

    [Fact]
    public void Resolve_Lost_LosesTheRun()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        RunEncounter encounter = run.OpenEncounter!;

        run.ResolveEncounter(
            encounter.Id,
            Guid.NewGuid(),
            EncounterOutcome.Lost,
            [new HeroFinalState(HeroId, 0, 0, true)],
            [],
            dungeon
        );

        run.Status.Should().Be(DungeonRunStatus.Lost);
    }

    [Fact]
    public void Resolve_Fled_TheMobsSurviveAndRestOnTheirNextTurn()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        RunEncounter encounter = run.OpenEncounter!;

        run.ResolveEncounter(
            encounter.Id,
            Guid.NewGuid(),
            EncounterOutcome.Fled,
            [new HeroFinalState(HeroId, 40, 5, false)],
            [],
            dungeon
        );

        RunMob mob = run.Mobs[0];
        mob.IsActive.Should().BeTrue();
        mob.InCombat.Should().BeFalse();
        mob.SkipsNextTurn.Should().BeTrue();
        run.Status.Should().Be(DungeonRunStatus.Active);
    }

    [Fact]
    public void Resolve_SameCombatTwice_AppliesItOnce()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        RunEncounter encounter = run.OpenEncounter!;
        Guid combatId = Guid.NewGuid();
        HeroFinalState[] heroes = [new HeroFinalState(HeroId, 50, 5, false)];
        run.ResolveEncounter(encounter.Id, combatId, EncounterOutcome.Won, heroes, [], dungeon);
        int turn = run.Turn;

        run.ResolveEncounter(encounter.Id, combatId, EncounterOutcome.Won, heroes, [], dungeon);

        run.Turn.Should().Be(turn);
    }

    [Fact]
    public void Resolve_AnotherCombatForASettledEncounter_IsRefused()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        RunEncounter encounter = run.OpenEncounter!;
        run.ResolveEncounter(encounter.Id, Guid.NewGuid(), EncounterOutcome.Won, [], [], dungeon);

        Action act = () =>
            run.ResolveEncounter(
                encounter.Id,
                Guid.NewGuid(),
                EncounterOutcome.Won,
                [],
                [],
                dungeon
            );

        act.Should().Throw<EncounterNotFoundException>();
    }

    [Fact]
    public void CombatUnavailable_KeepsThePartyBlockedUntilARetrySucceeds()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);
        Guid encounterId = run.OpenEncounter!.Id;

        run.MarkEncounterCombatUnavailable(encounterId);
        run.OpenEncounter!.Status.Should().Be(EncounterStatus.Failed);
        Action move = () => run.MoveHero(Direction.South, dungeon, MobRules.Default, Combat);
        move.Should().Throw<EncounterInProgressException>();

        Guid combatId = Guid.NewGuid();
        run.MarkEncounterCombatStarted(encounterId, combatId);
        run.OpenEncounter!.Status.Should().Be(EncounterStatus.Active);
        run.OpenEncounter.CombatId.Should().Be(combatId);
        run.OpenEncounter.Id.Should().Be(encounterId);
    }

    [Fact]
    public void WalkIntoTheBoss_OpensABossEncounter_AndWinningItDefeatsTheBoss()
    {
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H.X..");
        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        run.MoveHero(Direction.East, dungeon, MobRules.Default, Combat);

        RunEncounter encounter = run.OpenEncounter!;
        encounter.Trigger.Should().Be(EncounterTrigger.Boss);
        encounter.BossElementId.Should().NotBeNull();
        run.HeroPosition.Should().Be(new Position(2, 1));

        run.ResolveEncounter(encounter.Id, Guid.NewGuid(), EncounterOutcome.Won, [], [], dungeon);

        run.IsFloorBossDefeated.Should().BeTrue();
        run.Status.Should().Be(DungeonRunStatus.Won, "the arena is the last floor");
    }
}
