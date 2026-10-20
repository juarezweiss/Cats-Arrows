using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;

namespace CatsAndArrows.Tests
{
    // Issue #47: timing and ray recognition have no dependence on sleeps or hidden solution cells.
    public sealed class ArrowHoldControllerTests
    {
        private double now;
        private GameSession session;
        private BoardLayout layout;
        private ArrowHoldController hold;
        private readonly BoardCoordinate origin = new BoardCoordinate(2, 2);
        private Vector2 Center(BoardCoordinate cell) => layout.GetCellRect(cell).center;

        [SetUp]
        public void SetUp()
        {
            now = 12;
            session = MakeSession(5, origin, Direction.DownRight);
            layout = BoardLayout.Calculate(390, 390, 5);
            hold = new ArrowHoldController(session, layout, () => now);
        }

        [Test]
        public void HoldActivatesAt350MillisecondsOnceWithoutMutatingMarks()
        {
            var before = session.Marks.ToArray();
            now = 0;
            Assert.That(hold.Begin(17, Center(origin)), Is.True);
            now = .349;
            Assert.That(hold.Tick(), Is.False);
            Assert.That(hold.IsHighlighted, Is.False);
            now = .350;
            Assert.That(hold.Tick(), Is.True);
            now = 100;
            Assert.That(hold.Tick(), Is.False);
            Assert.That(hold.IsHighlighted, Is.True);
            hold.End(17);
            Assert.That(hold.HasActivePointer || hold.IsHighlighted, Is.False);
            Assert.That(hold.Path, Is.Empty);
            Assert.That(hold.Tick(), Is.False);
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void EligibilityAndActivationHooksKeepTheExistingTimingAndOrdinaryDefault()
        {
            var other = new BoardCoordinate(0, 0);
            var puzzle = new PuzzleDefinition(5, 68, "issue-68-tutorial-hold",
                new[] { new BoardCoordinate(4, 4), new BoardCoordinate(0, 4) },
                new[] { new ArrowClue(origin, Direction.DownRight), new ArrowClue(other, Direction.Right) });
            var game = new GameSession(puzzle);
            var activations = 0;
            var activatedOrigin = default(BoardCoordinate);
            var allowAnyArrow = false;
            now = 0;
            var tutorialHold = new ArrowHoldController(game, layout, () => now,
                canBegin: coordinate => allowAnyArrow || coordinate.Equals(origin),
                activated: coordinate => { activations++; activatedOrigin = coordinate; });

            Assert.That(tutorialHold.Begin(1, Center(other)), Is.False);
            Assert.That(tutorialHold.Begin(1, Center(origin)), Is.True);
            now = .349;
            Assert.That(tutorialHold.Tick(), Is.False);
            Assert.That(activations, Is.Zero);
            tutorialHold.End(1);
            now = 10;
            Assert.That(tutorialHold.Tick(), Is.False);
            Assert.That(activations, Is.Zero);

            Assert.That(tutorialHold.Begin(2, Center(origin)), Is.True);
            now += 1;
            Assert.That(tutorialHold.Tick(), Is.True);
            Assert.That(tutorialHold.Tick(), Is.False);
            Assert.That(activations, Is.EqualTo(1));
            Assert.That(activatedOrigin, Is.EqualTo(origin));
            tutorialHold.End(2);

            allowAnyArrow = true;
            var before = game.Marks.ToArray();
            Assert.That(tutorialHold.Begin(3, Center(other)), Is.True);
            now += ArrowHoldController.HoldSeconds + .001;
            Assert.That(tutorialHold.Tick(), Is.True);
            Assert.That(activatedOrigin, Is.EqualTo(other));
            tutorialHold.End(3);
            Assert.That(tutorialHold.IsHighlighted, Is.False);
            Assert.That(tutorialHold.Path, Is.Empty);
            Assert.That(game.Marks, Is.EquivalentTo(before));

            var ordinaryHold = new ArrowHoldController(game, layout, () => now);
            Assert.That(ordinaryHold.Begin(4, Center(other)), Is.True,
                "No eligibility hook must preserve ordinary-board arrow holds.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelBeforeOrAfterActivationPreventsLateCallbacksAndRelease(bool activate)
        {
            hold.Begin(1, Center(origin));
            now += activate ? 1 : .349;
            hold.Tick();
            hold.Cancel();
            now += 20;
            Assert.That(hold.Tick(), Is.False);
            hold.End(1);
            Assert.That(hold.HasActivePointer || hold.IsHighlighted, Is.False);
            Assert.That(session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
            Assert.That(hold.Begin(2, Center(origin)), Is.True);
            Assert.That(hold.Tick(), Is.False);
        }

        [Test]
        public void EarlyReleaseAndAlternatingPressesRestartTheTimer()
        {
            hold.Begin(1, Center(origin)); now += .349; hold.End(1);
            now += 10; Assert.That(hold.Tick(), Is.False);
            hold.Begin(2, Center(origin)); now += .349;
            Assert.That(hold.Tick(), Is.False);
            hold.End(2);
            hold.Begin(3, Center(origin)); now += 1;
            Assert.That(hold.Tick(), Is.True);
        }

        [Test]
        public void SecondPointerCannotStealMoveOrReleaseTheOwningHold()
        {
            hold.Begin(17, Center(origin));
            Assert.That(hold.Begin(18, Center(origin)), Is.False);
            hold.Move(18, Vector2.zero); hold.End(18);
            Assert.That(hold.ActivePointerId, Is.EqualTo(17));
            now += 1; Assert.That(hold.Tick(), Is.True);
            hold.End(18); Assert.That(hold.IsHighlighted, Is.True);
            hold.End(17); Assert.That(hold.IsHighlighted, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SmallJitterIsAllowedButEightUnitMovementCancelsWithoutHandoff(bool activate)
        {
            hold.Begin(1, Center(origin));
            if (activate) { now += 1; hold.Tick(); }
            hold.Move(1, Center(origin) + new Vector2(7.9f, 0));
            Assert.That(hold.HasActivePointer, Is.True);
            hold.Move(1, Center(origin) + new Vector2(8, 0));
            Assert.That(hold.HasActivePointer || hold.IsHighlighted, Is.False);
            hold.Move(1, Center(new BoardCoordinate(2, 3))); hold.End(1);
            Assert.That(session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
        }

        [Test]
        public void LeavingOriginEvenWithinJitterToleranceCancels()
        {
            var rect = layout.GetCellRect(origin);
            hold.Begin(1, new Vector2(rect.xMax - 1, rect.center.y));
            hold.Move(1, new Vector2(rect.xMax + 1, rect.center.y));
            Assert.That(hold.HasActivePointer, Is.False);
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void EveryDirectionHighlightsExactlyItsEntireRayWithoutWrappingOrLookingAtMarks(int size)
        {
            var steps = new Dictionary<Direction, Vector2Int>
            {
                { Direction.Up, new Vector2Int(-1, 0) }, { Direction.Down, new Vector2Int(1, 0) },
                { Direction.Left, new Vector2Int(0, -1) }, { Direction.Right, new Vector2Int(0, 1) },
                { Direction.UpLeft, new Vector2Int(-1, -1) }, { Direction.UpRight, new Vector2Int(-1, 1) },
                { Direction.DownLeft, new Vector2Int(1, -1) }, { Direction.DownRight, new Vector2Int(1, 1) }
            };
            foreach (var step in steps)
            {
                var puzzle = MakeSession(size, origin, step.Key);
                var geometry = BoardLayout.Calculate(320, 320, size);
                var controller = new ArrowHoldController(puzzle, geometry, () => now);
                Assert.That(controller.Begin(1, geometry.GetCellRect(origin).center), Is.True);
                var expected = new List<BoardCoordinate>();
                for (int row = origin.Row + step.Value.x, column = origin.Column + step.Value.y;
                     row >= 0 && row < size && column >= 0 && column < size;
                     row += step.Value.x, column += step.Value.y)
                    expected.Add(new BoardCoordinate(row, column));
                Assert.That(controller.Path, Is.EqualTo(expected), step.Key.ToString());
                Assert.That(controller.Path, Has.No.Member(origin));
            }
        }

        [Test]
        public void RayIncludesInterveningArrowProtectionXPawAndContinuesPastAllOfThem()
        {
            var arrows = new[] { new ArrowClue(new BoardCoordinate(0, 0), Direction.Right),
                new ArrowClue(new BoardCoordinate(0, 1), Direction.Down) };
            var puzzle = new PuzzleDefinition(7, 47, "issue-47-mixed-ray", new[] { new BoardCoordinate(6, 6) }, arrows);
            var game = new GameSession(puzzle, new[] { new BoardCoordinate(0, 2) });
            game.Tap(new BoardCoordinate(0, 3));
            game.Tap(new BoardCoordinate(0, 4)); game.Tap(new BoardCoordinate(0, 4));
            var before = game.Marks.ToArray();
            var geometry = BoardLayout.Calculate(320, 320, 7);
            var controller = new ArrowHoldController(game, geometry, () => now);
            controller.Begin(1, geometry.GetCellRect(new BoardCoordinate(0, 0)).center);
            now += 1; controller.Tick();
            Assert.That(controller.Path, Is.EqualTo(Enumerable.Range(1, 6).Select(column => new BoardCoordinate(0, column))));
            controller.End(1);
            Assert.That(game.Marks, Is.EquivalentTo(before));
        }

        [TestCase(Direction.Up, 0)]
        [TestCase(Direction.UpRight, 0)]
        [TestCase(Direction.DownRight, 1)]
        public void DefensiveEdgeRaysCanBeEmptyOrOneCell(Direction direction, int count)
        {
            var cell = new BoardCoordinate(0, 0);
            var game = MakeSession(2, cell, direction);
            var geometry = BoardLayout.Calculate(320, 320, 2);
            var controller = new ArrowHoldController(game, geometry, () => now);
            Assert.That(controller.Begin(1, geometry.GetCellRect(cell).center), Is.True);
            now += 1; Assert.That(controller.Tick(), Is.True);
            Assert.That(controller.Path.Count, Is.EqualTo(count));
        }

        [Test]
        public void NonArrowCompletedAndInvalidCoordinatesCannotBegin()
        {
            Assert.That(hold.Begin(1, Center(new BoardCoordinate(0, 0))), Is.False);
            Assert.That(hold.Begin(1, new Vector2(float.NaN, 0)), Is.False);
            Assert.That(hold.Begin(1, Vector2.zero), Is.False);
            foreach (var paw in session.Puzzle.SolutionPaws) { session.Tap(paw); session.Tap(paw); }
            Assert.That(hold.Begin(1, Center(origin)), Is.False);
        }

        [Test]
        public void CompletionOrInvalidClockDuringPendingHoldCancelsIt()
        {
            hold.Begin(1, Center(origin));
            foreach (var paw in session.Puzzle.SolutionPaws) { session.Tap(paw); session.Tap(paw); }
            now += 2; Assert.That(hold.Tick(), Is.False);
            Assert.That(hold.HasActivePointer, Is.False);
            session = MakeSession(5, origin, Direction.DownRight);
            hold = new ArrowHoldController(session, layout, () => now);
            hold.Begin(2, Center(origin)); now = double.NaN;
            Assert.That(hold.Tick(), Is.False); Assert.That(hold.HasActivePointer, Is.False);
        }

        private static GameSession MakeSession(int size, BoardCoordinate arrow, Direction direction) =>
            new GameSession(new PuzzleDefinition(size, 47, "issue-47-hold-test",
                new[] { new BoardCoordinate(size - 1, 0) }, new[] { new ArrowClue(arrow, direction) }));
    }
}
