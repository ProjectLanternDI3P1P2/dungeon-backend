using Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;
using FluentAssertions;
using FluentValidation.Results;

namespace Dungeon.Test.Features.DungeonRunUseCase.CreateDungeonRun;

public class CreateDungeonRunValidatorTests
{
    private readonly CreateDungeonRunValidator _validator = new();

    [Fact]
    public void Validate_DistinctHeroes_IsValid()
    {
        // Arrange
        var command = new CreateDungeonRunCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            [Guid.NewGuid(), Guid.NewGuid()]
        );

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoHeroList_IsValid()
    {
        // Arrange: a run started without Player, in development.
        var command = new CreateDungeonRunCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyHeroId_IsRejected()
    {
        // Arrange
        var command = new CreateDungeonRunCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            [Guid.Empty]
        );

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result
            .Errors.Should()
            .ContainSingle(error => error.ErrorMessage == "HeroIds must not contain an empty id.");
    }

    [Fact]
    public void Validate_SameHeroTwice_IsRejected()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        var command = new CreateDungeonRunCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            [heroId, heroId]
        );

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result
            .Errors.Should()
            .ContainSingle(error => error.ErrorMessage == "HeroIds must list each hero once.");
    }
}
