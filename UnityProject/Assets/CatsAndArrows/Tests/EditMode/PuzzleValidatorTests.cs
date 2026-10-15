using System;
using System.Linq;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class PuzzleValidatorTests
    {
        [Test]
        public void Issue32MediumAllowsZeroAndThreeButRetainsExistingPerAxisLimit()
        {
            var cats = Enumerable.Range(1, 5).Select(i => new BoardCoordinate(i, i))
                .Concat(new[] { new BoardCoordinate(1, 2), new BoardCoordinate(1, 3) }).ToArray();
            var valid = new PuzzleDefinition(6, 32, "fixture", cats, Array.Empty<ArrowClue>());
            Assert.That(valid.GetRowCatCount(0), Is.Zero);
            Assert.That(valid.GetColumnCatCount(0), Is.Zero);
            Assert.That(valid.GetRowCatCount(1), Is.EqualTo(3));
            Assert.That(PuzzleValidator.Validate(valid, DifficultyProfile.Medium).IsValid, Is.True);
            var extraZero = new PuzzleDefinition(6, 32, "fixture", cats.Where(c => c.Row != 5), Array.Empty<ArrowClue>());
            Assert.That(PuzzleValidator.Validate(extraZero, DifficultyProfile.Medium).IsValid, Is.False);
            var excess = new PuzzleDefinition(6, 32, "fixture", cats.Concat(new[] { new BoardCoordinate(1, 4) }), Array.Empty<ArrowClue>());
            Assert.That(PuzzleValidator.Validate(excess, DifficultyProfile.Medium).IsValid, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Issue32HardAcceptsAThreeTotalInEitherAxis(bool row)
        {
            var cats = Enumerable.Range(0, 7).Select(i => new BoardCoordinate(i, i)).Concat(row
                ? new[] { new BoardCoordinate(0, 1), new BoardCoordinate(0, 2) }
                : new[] { new BoardCoordinate(1, 0), new BoardCoordinate(2, 0) }).ToArray();
            var valid = new PuzzleDefinition(7, 32, "fixture", cats, Array.Empty<ArrowClue>());
            Assert.That(PuzzleValidator.Validate(valid, DifficultyProfile.Hard).IsValid, Is.True);
            var zero = new PuzzleDefinition(7, 32, "fixture", cats.Where(c => c.Row != 6), Array.Empty<ArrowClue>());
            Assert.That(PuzzleValidator.Validate(zero, DifficultyProfile.Hard).IsValid, Is.False);
            var missingThree = new PuzzleDefinition(7, 32, "fixture", Enumerable.Range(0, 7).Select(i => new BoardCoordinate(i, i)), Array.Empty<ArrowClue>());
            Assert.That(PuzzleValidator.Validate(missingThree, DifficultyProfile.Hard).Errors, Has.Some.Contains("exactly 3"));
        }

        [Test]
        public void Issue35HardRejectsFourAlongsideOtherwiseValidRequiredThreeLine()
        {
            var cats = Enumerable.Range(0, 7).Select(i => new BoardCoordinate(i, i)).Concat(new[] {
                new BoardCoordinate(0, 1), new BoardCoordinate(0, 2), new BoardCoordinate(0, 3),
                new BoardCoordinate(1, 2), new BoardCoordinate(1, 3) }).ToArray();
            var four = new PuzzleDefinition(7, 35, "fixture", cats, Array.Empty<ArrowClue>());
            Assert.That(four.GetRowCatCount(0), Is.EqualTo(4));
            Assert.That(four.GetRowCatCount(1), Is.EqualTo(3));
            Assert.That(PuzzleValidator.Validate(four, DifficultyProfile.Hard).Errors, Has.Some.Contains("max is 3"));
        }

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
