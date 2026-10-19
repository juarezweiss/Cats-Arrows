using System;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;

namespace CatsAndArrows.Tests
{
    // Issue #7: completed pointer gestures adapt visible geometry into domain commands.
    public sealed class BoardGestureControllerTests
    {
        private GameSession _session;
        private BoardLayout _layout;
        private BoardGestureController _gesture;
        private static BoardCoordinate Cell(int row, int column) => new BoardCoordinate(row, column);
        private Vector2 Center(int row, int column) => _layout.GetCellRect(Cell(row, column)).center;

        [SetUp]
        public void SetUp()
        {
            _session = MakeSession();
            _layout = BoardLayout.Calculate(390, 390, 5);
            _gesture = new BoardGestureController(_session, _layout, 8);
        }

        [Test]
        public void ThreeDiscreteReleasesCycleEmptyXPawEmptyExactlyOnceEach()
        {
            var point = Center(0, 0);
            foreach (var expected in new[] { PlayerMark.X, PlayerMark.Paw, PlayerMark.Empty })
            {
                Assert.That(_gesture.Begin(1, point), Is.True);
                Assert.That(_session.GetMark(Cell(0, 0)), Is.Not.EqualTo(expected));
                Assert.That(_gesture.End(1, point), Is.True);
                Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(expected));
                Assert.That(_gesture.End(1, point), Is.False);
                Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(expected));
                Assert.That(_gesture.HasActivePointer, Is.False);
            }
        }

        [Test]
        public void HoldingWithoutMovementDoesNotMutateUntilRelease()
        {
            var point = Center(0, 0);
            Assert.That(_gesture.Begin(1, point), Is.True);
            for (var frame = 0; frame < 120; frame++) Assert.That(_gesture.Move(1, point), Is.False);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
            Assert.That(_gesture.End(1, point), Is.True);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.X));
        }

        [Test]
        public void SecondPointerCannotReplaceOrFinishTheCapturedGesture()
        {
            Assert.That(_gesture.Begin(17, Center(0, 0)), Is.True);
            Assert.That(_gesture.ActivePointerId, Is.EqualTo(17));
            Assert.That(_gesture.Begin(18, Center(2, 2)), Is.False);
            Assert.That(_gesture.Move(18, Center(0, 4)), Is.False);
            Assert.That(_gesture.End(18, Center(0, 4)), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.True);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
            Assert.That(_gesture.End(17, Center(0, 0)), Is.True);
            Assert.That(_session.GetMark(Cell(2, 2)), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void CancelBeforeReleaseProducesNoTapAndAllowsANewGesture()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Cancel();
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_gesture.IsDragging, Is.False);
            Assert.That(_gesture.End(1, Center(0, 0)), Is.False);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(_gesture.Begin(2, Center(2, 2)), Is.True);
            Assert.That(_gesture.End(2, Center(2, 2)), Is.True);
        }

        [Test]
        public void SubThresholdMotionStaysATapButThresholdMotionBecomesDrag()
        {
            var point = Center(0, 0);
            _gesture.Begin(1, point);
            Assert.That(_gesture.Move(1, point + new Vector2(7.9f, 0)), Is.False);
            Assert.That(_gesture.IsDragging, Is.False);
            Assert.That(_gesture.End(1, point), Is.True);
            _session.Clear();
            _gesture.Begin(2, point);
            Assert.That(_gesture.Move(2, point + new Vector2(8, 0)), Is.True);
            Assert.That(_gesture.IsDragging, Is.True);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.X));
            Assert.That(_gesture.End(2, point), Is.False);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.X));
        }

        [Test]
        public void DiagonalThresholdUsesTotalDisplacementRatherThanEitherAxisAlone()
        {
            var point = Center(0, 0);
            _gesture.Begin(1, point);
            Assert.That(_gesture.Move(1, point + new Vector2(5, 5)), Is.False);
            Assert.That(_gesture.Move(1, point + new Vector2(6, 6)), Is.True);
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Horizontal));
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.X));
            Assert.That(_gesture.End(1, point), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SparseMotionEventsMarkEveryTraversedCellIncludingOrigin(bool horizontal)
        {
            // Column zero contains an arrow; use an entirely eligible path here.
            _gesture.Begin(1, horizontal ? Center(0, 0) : Center(0, 1));
            var end = horizontal ? Center(0, 4) : Center(4, 1);
            Assert.That(_gesture.Move(1, end), Is.True);
            Assert.That(_gesture.IsDragging, Is.True);
            for (var index = 0; index < 5; index++)
                Assert.That(_session.GetMark(horizontal ? Cell(0, index) : Cell(index, 1)), Is.EqualTo(PlayerMark.X));
            Assert.That(_session.Marks.Count(pair => pair.Value == PlayerMark.X), Is.EqualTo(5));
            Assert.That(_gesture.End(1, end), Is.False);
        }

        [Test]
        public void ReleaseCanSupplyFirstSparseDragEventWithoutAddingATap()
        {
            _gesture.Begin(1, Center(0, 0));
            Assert.That(_gesture.End(1, Center(0, 4)), Is.True);
            Assert.That(_session.Marks.Count(pair => pair.Value == PlayerMark.X), Is.EqualTo(5));
            Assert.That(_session.Marks.Values.Contains(PlayerMark.Paw), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
        }

        [Test]
        public void ReversingAlongLockedAxisDoesNotCycleOrRevisitMarks()
        {
            _gesture.Begin(1, Center(0, 2));
            Assert.That(_gesture.Move(1, Center(0, 4)), Is.True);
            Assert.That(_gesture.Move(1, Center(0, 0)), Is.True);
            Assert.That(_gesture.Move(1, Center(0, 4)), Is.False);
            Assert.That(_gesture.End(1, Center(0, 2)), Is.False);
            Assert.That(_session.Marks.Count(pair => pair.Value == PlayerMark.X), Is.EqualTo(5));
            Assert.That(_session.Marks.Values.Contains(PlayerMark.Paw), Is.False);
        }

        [Test]
        public void DiagonalMotionLocksDominantAxisAndNeverMarksAnotherRow()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Move(1, Center(2, 4));
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Horizontal));
            _gesture.Move(1, Center(4, 2));
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Horizontal));
            Assert.That(_session.Marks.Where(pair => pair.Value == PlayerMark.X).All(pair => pair.Key.Row == 0), Is.True);
        }

        [Test]
        public void EqualDiagonalDisplacementDeterministicallyChoosesHorizontal()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Move(1, Center(2, 2));
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Horizontal));
            Assert.That(_session.GetMark(Cell(0, 2)), Is.EqualTo(PlayerMark.X));
            Assert.That(_session.GetMark(Cell(2, 0)), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void VerticalAxisRemainsLockedWhenLaterMotionBecomesHorizontal()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Move(1, Center(3, 1));
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Vertical));
            _gesture.Move(1, Center(2, 4));
            Assert.That(_gesture.Axis, Is.EqualTo(BoardDragAxis.Vertical));
            Assert.That(_session.Marks.Where(pair => pair.Value == PlayerMark.X).All(pair => pair.Key.Column == 0), Is.True);
        }

        [TestCase(PlayerMark.X)]
        [TestCase(PlayerMark.Paw)]
        public void DragFromAnExistingMarkCancelsWithoutChangingAnyCell(PlayerMark originMark)
        {
            _session.Tap(Cell(0, 0));
            if (originMark == PlayerMark.Paw) _session.Tap(Cell(0, 0));
            var before = _session.Marks.ToArray();
            Assert.That(_gesture.Begin(1, Center(0, 0)), Is.True);
            Assert.That(_gesture.Move(1, Center(0, 4)), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_gesture.End(1, Center(0, 0)), Is.False);
            Assert.That(_session.Marks, Is.EquivalentTo(before));
        }

        [Test]
        public void DragAcrossMixedCellsChangesOnlyEligibleEmptyCells()
        {
            _session.Tap(Cell(1, 3));
            _session.Tap(Cell(1, 4));
            _session.Tap(Cell(1, 4));
            _gesture.Begin(1, Center(1, 1));
            _gesture.Move(1, Center(1, 4));
            _gesture.Move(1, Center(1, 0));
            Assert.That(_session.GetMark(Cell(1, 1)), Is.EqualTo(PlayerMark.X));
            Assert.That(_session.GetMark(Cell(1, 3)), Is.EqualTo(PlayerMark.X));
            Assert.That(_session.GetMark(Cell(1, 4)), Is.EqualTo(PlayerMark.Paw));
            Assert.That(_session.Marks.ContainsKey(Cell(1, 0)), Is.False);
            Assert.That(_session.Marks.ContainsKey(Cell(1, 2)), Is.False);
            Assert.That(_session.Marks.Count(pair => pair.Value == PlayerMark.X), Is.EqualTo(2));
        }

        [TestCase(1, 0)]
        [TestCase(1, 2)]
        public void ArrowAndProtectedOriginsCannotCaptureAPointer(int row, int column)
        {
            Assert.That(_gesture.Begin(1, Center(row, column)), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_gesture.End(1, Center(row, column)), Is.False);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
        }

        [Test]
        public void LeavingGridCancelsWithoutPaintingTheUnobservedPathOrTappingOnReturn()
        {
            _gesture.Begin(1, Center(0, 0));
            Assert.That(_gesture.Move(1, new Vector2(_layout.Width + 1, Center(0, 0).y)), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_gesture.End(1, Center(0, 0)), Is.False);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
        }

        [Test]
        public void LeavingGridAfterDragRetainsOnlyAlreadyAppliedMarks()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Move(1, Center(0, 2));
            Assert.That(_gesture.Move(1, new Vector2(-1, Center(0, 0).y)), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_gesture.End(1, Center(0, 4)), Is.False);
            Assert.That(_session.Marks.Count(pair => pair.Value == PlayerMark.X), Is.EqualTo(3));
        }

        [Test]
        public void GapsDoNotBecomeTapTargetsButDragBridgesThem()
        {
            var first = _layout.GetCellRect(Cell(0, 0));
            var gap = new Vector2(first.xMax + _layout.Gap / 2, first.center.y);
            Assert.That(_gesture.Begin(1, gap), Is.False);
            _gesture.Begin(2, first.center);
            Assert.That(_gesture.Move(2, gap), Is.True);
            Assert.That(_gesture.HasActivePointer, Is.True);
            Assert.That(_gesture.Move(2, Center(0, 2)), Is.True);
            Assert.That(_session.GetMark(Cell(0, 1)), Is.EqualTo(PlayerMark.X));
        }

        [Test]
        public void TapReleaseInAGapDoesNotCycleTheOrigin()
        {
            var first = _layout.GetCellRect(Cell(0, 0));
            var nearEdge = new Vector2(first.xMax - 1, first.center.y);
            var gap = new Vector2(first.xMax + 1, first.center.y);
            _gesture.Begin(1, nearEdge);
            Assert.That(_gesture.End(1, gap), Is.False);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void SubThresholdReleaseInAnotherCellDoesNotCycleEitherCell()
        {
            var first = _layout.GetCellRect(Cell(0, 0));
            var second = _layout.GetCellRect(Cell(0, 1));
            _gesture.Begin(1, new Vector2(first.xMax - 0.1f, first.center.y));
            Assert.That(_gesture.End(1, new Vector2(second.xMin + 0.1f, second.center.y)), Is.False);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(_session.GetMark(Cell(0, 1)), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void FinalPawTapCompletesSessionAndBlocksAllLaterInput()
        {
            _session.Tap(Cell(4, 3));
            _session.Tap(Cell(4, 3));
            _session.Tap(Cell(4, 4));
            _gesture.Begin(1, Center(4, 4));
            Assert.That(_gesture.End(1, Center(4, 4)), Is.True);
            Assert.That(_session.IsCompleted, Is.True);
            var before = _session.Marks.ToArray();
            Assert.That(_gesture.Begin(2, Center(0, 0)), Is.False);
            Assert.That(_gesture.Move(2, Center(0, 4)), Is.False);
            Assert.That(_gesture.End(2, Center(0, 0)), Is.False);
            Assert.That(_session.Marks, Is.EquivalentTo(before));
        }

        [Test]
        public void SessionCompletingDuringHeldPointerPreventsItsDeferredTap()
        {
            _gesture.Begin(1, Center(0, 0));
            foreach (var paw in _session.Puzzle.SolutionPaws) { _session.Tap(paw); _session.Tap(paw); }
            Assert.That(_gesture.End(1, Center(0, 0)), Is.False);
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(_gesture.HasActivePointer, Is.False);
        }

        [Test]
        public void CancelAndClearThenNewSessionDoNotCarryOldPointerState()
        {
            _gesture.Begin(1, Center(0, 0));
            _gesture.Move(1, Center(0, 2));
            _gesture.Cancel();
            Assert.That(_session.Clear(), Is.True);
            Assert.That(_gesture.End(1, Center(0, 0)), Is.False);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
            var next = MakeSession();
            var nextGesture = new BoardGestureController(next, _layout, 8);
            Assert.That(nextGesture.End(1, Center(0, 0)), Is.False);
            Assert.That(nextGesture.Begin(2, Center(0, 0)), Is.True);
            Assert.That(nextGesture.End(2, Center(0, 0)), Is.True);
            Assert.That(next.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.X));
            Assert.That(_session.GetMark(Cell(0, 0)), Is.EqualTo(PlayerMark.Empty));
        }

        [TestCase(320, 568, 5)]
        [TestCase(390, 844, 5)]
        [TestCase(430, 932, 5)]
        [TestCase(390, 844, 7)]
        [TestCase(390, 844, 10)]
        public void HitTestingResolvesEveryTileAndRejectsCluesGapsAndOuterEdges(int width, int height, int size)
        {
            var layout = BoardLayout.Calculate(width, height, size);
            var puzzle = new PuzzleDefinition(size, 7, "issue-7-hit-test", new[] { Cell(size - 1, size - 1) }, Array.Empty<ArrowClue>());
            var gesture = new BoardGestureController(new GameSession(puzzle), layout);
            for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
            {
                var cell = Cell(row, column);
                var rect = layout.GetCellRect(cell);
                Assert.That(gesture.TryGetCell(rect.center, out var actual), Is.True);
                Assert.That(actual, Is.EqualTo(cell));
                Assert.That(gesture.TryGetCell(rect.min, out actual), Is.True);
                Assert.That(actual, Is.EqualTo(cell));
                Assert.That(gesture.TryGetCell(new Vector2(rect.xMax, rect.center.y), out _), Is.False);
                Assert.That(gesture.TryGetCell(new Vector2(rect.center.x, rect.yMax), out _), Is.False);
                if (column < size - 1)
                    Assert.That(gesture.TryGetCell(new Vector2(rect.xMax + layout.Gap / 2, rect.center.y), out _), Is.False);
                if (row < size - 1)
                    Assert.That(gesture.TryGetCell(new Vector2(rect.center.x, rect.yMax + layout.Gap / 2), out _), Is.False);
            }
            Assert.That(gesture.TryGetCell(Vector2.zero, out _), Is.False);
            Assert.That(gesture.TryGetCell(layout.GetRowClueRect(0).center, out _), Is.False);
            Assert.That(gesture.TryGetCell(layout.GetColumnClueRect(0).center, out _), Is.False);
            Assert.That(gesture.TryGetCell(new Vector2(layout.Width, layout.Height), out _), Is.False);
        }

        [TestCase(float.NaN, 10)]
        [TestCase(10, float.NaN)]
        [TestCase(float.PositiveInfinity, 10)]
        [TestCase(10, float.NegativeInfinity)]
        public void NonFiniteCoordinatesAreRejectedAndCancelAnActivePointer(float x, float y)
        {
            var position = new Vector2(x, y);
            Assert.That(_gesture.TryGetCell(position, out _), Is.False);
            Assert.That(_gesture.Begin(1, position), Is.False);
            _gesture.Begin(2, Center(0, 0));
            Assert.That(_gesture.Move(2, position), Is.False);
            Assert.That(_gesture.HasActivePointer, Is.False);
            Assert.That(_session.Marks.Values.All(mark => mark == PlayerMark.Empty), Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDragThresholdCannotCreateAController(float threshold)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardGestureController(_session, _layout, threshold));
        }

        [Test]
        public void MissingSessionAndMismatchedOrDefaultLayoutCannotCreateAController()
        {
            Assert.Throws<ArgumentNullException>(() => new BoardGestureController(null, _layout));
            Assert.Throws<ArgumentException>(() => new BoardGestureController(_session, default));
            Assert.Throws<ArgumentException>(() => new BoardGestureController(_session, BoardLayout.Calculate(390, 390, 7)));
        }

        private static GameSession MakeSession() => new GameSession(new PuzzleDefinition(5, 7, "issue-7-gesture-test",
            new[] { Cell(4, 3), Cell(4, 4) },
            new[] { new ArrowClue(Cell(1, 0), Direction.DownRight) }), new[] { Cell(1, 2) });
    }
}
