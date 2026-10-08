using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class PuzzleValidatorTests
    {
        [TestCase(Direction.UpLeft, 0, 0, 0, 1)]
        [TestCase(Direction.UpRight, 0, 4, 0, 3)]
        [TestCase(Direction.DownLeft, 4, 0, 4, 1)]
        [TestCase(Direction.DownRight, 4, 4, 4, 3)]
        public void Issue14DiagonalRequiresAnExactRayRatherThanTheWholeQuadrant(
            Direction direction, int catRow, int catColumn, int offRow, int offColumn)
        {
            var arrow = new ArrowClue(new BoardCoordinate(2, 2), direction);
            var profile = new DifficultyProfile("geometry-fixture", 5, 5, 5, 5);
            var exact = new PuzzleDefinition(5, 14, "fixture", new[] { new BoardCoordinate(catRow, catColumn) }, new[] { arrow });
            var offRay = new PuzzleDefinition(5, 14, "fixture", new[] { new BoardCoordinate(offRow, offColumn) }, new[] { arrow });
            Assert.That(PuzzleValidator.Validate(exact, profile).IsValid, Is.True);
            Assert.That(PuzzleValidator.Validate(offRay, profile).IsValid, Is.False);
            Assert.That(PuzzleValidator.Validate(offRay, profile).Errors, Has.Some.Contains("has no solution cat"));
        }

        [Test]
        public void RejectsCellThatIsBothArrowAndCat()
        {
            var shared = new BoardCoordinate(1, 1);
            var puzzle = new PuzzleDefinition(
                5,
                123,
                "test",
                new[] { shared, new BoardCoordinate(1, 3), new BoardCoordinate(3, 0), new BoardCoordinate(4, 4) },
                new[] { new ArrowClue(shared, Direction.Right) });

            var result = PuzzleValidator.Validate(puzzle, DifficultyProfile.Easy);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("both an arrow and a solution cat"));
        }

        [Test]
        public void AcceptsTruthfulArrowAndLineLimits()
        {
            var puzzle = new PuzzleDefinition(
                5,
                42,
                "test",
                new[]
                {
                    new BoardCoordinate(0, 4),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 3),
                    new BoardCoordinate(3, 0),
                    new BoardCoordinate(4, 2)
                },
                new[] { new ArrowClue(new BoardCoordinate(0, 1), Direction.Right) });

            var result = PuzzleValidator.Validate(puzzle, DifficultyProfile.Easy);

            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Errors));
        }

        [Test]
        public void RejectsArrowWithNoCatInPointedSegment()
        {
            var puzzle = new PuzzleDefinition(
                5,
                42,
                "test",
                new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(3, 3),
                    new BoardCoordinate(4, 4)
                },
                new[] { new ArrowClue(new BoardCoordinate(0, 3), Direction.Right) });

            var result = PuzzleValidator.Validate(puzzle, DifficultyProfile.Easy);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("has no solution cat"));
        }
    }
}
