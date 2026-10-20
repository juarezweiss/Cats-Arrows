using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #26: fixed lesson invariants and a replay through ordinary tap/drag rules.
    public sealed class TutorialControllerTests
    {
        private static BoardCoordinate C(int row, int column) => new BoardCoordinate(row, column);

        [Test]
        public void PawTutorialStepsKeepLegacyNumericValues()
        {
            Assert.That((int)TutorialStep.PawState, Is.EqualTo(6));
            Assert.That((int)TutorialStep.FirstPaw, Is.EqualTo(12));
            Assert.That((int)TutorialStep.SecondPaw, Is.EqualTo(14));
            Assert.That((int)TutorialStep.DiagonalPaw, Is.EqualTo(16));
            Assert.That((int)TutorialStep.FourthPaw, Is.EqualTo(17));
            Assert.That((int)TutorialStep.FinalPaw, Is.EqualTo(19));
            Assert.That((int)TutorialStep.Complete, Is.EqualTo(21));
            Assert.That((int)TutorialStep.ArrowHoldIntro, Is.EqualTo(22));
        }

        [Test]
        public void FixtureHasExactApprovedCountsArrowsPawsAndDerivedProtection()
        {
            var puzzle = TutorialFixture.CreatePuzzle();
            Assert.That(puzzle.Size, Is.EqualTo(5));
            Assert.That(puzzle.Seed, Is.EqualTo(26));
            Assert.That(puzzle.GeneratorVersion, Is.EqualTo("tutorial-v1"));
            Assert.That(Enumerable.Range(0, 5).Select(puzzle.GetRowPawCount), Is.EqualTo(new[] { 0, 1, 2, 1, 1 }));
            Assert.That(Enumerable.Range(0, 5).Select(puzzle.GetColumnPawCount), Is.EqualTo(new[] { 0, 2, 1, 1, 1 }));
            Assert.That(puzzle.SolutionPaws, Is.EquivalentTo(new[] { C(1, 4), C(4, 2), C(2, 1), C(2, 3), C(3, 1) }));
            Assert.That(puzzle.Arrows.Count, Is.EqualTo(3));
            Assert.That(puzzle.Arrows[C(1, 3)], Is.EqualTo(Direction.Right));
            Assert.That(puzzle.Arrows[C(3, 2)], Is.EqualTo(Direction.Down));
            Assert.That(puzzle.Arrows[C(0, 3)], Is.EqualTo(Direction.DownLeft));
            foreach (var arrow in puzzle.Arrows)
            {
                Assert.That(puzzle.IsSolutionPaw(arrow.Key), Is.False);
                Assert.That(puzzle.GetDirectionalSegment(arrow.Key, arrow.Value).Any(puzzle.IsSolutionPaw), Is.True);
            }
            Assert.That(TutorialFixture.CreateSession().ProtectedCells,
                Is.EquivalentTo(new[] { C(0, 2), C(1, 0), C(1, 1), C(1, 2), C(2, 2) }));
        }

        [Test]
        public void FixturePassesCurrentEasyHumanAndIndependentUniquenessContracts()
        {
            var puzzle = TutorialFixture.CreatePuzzle();
            var assessment = PuzzleSolvabilityAssessment.Assess(puzzle, DifficultyProfile.Easy);
            Assert.That(assessment.IsAccepted, Is.True, string.Join("; ", assessment.Errors));
            var clues = PuzzleClues.FromPuzzle(puzzle);
            var human = new HumanDeductionSolver().Solve(clues);
            var unique = new UniquenessVerifier().Verify(clues);
            Assert.That(human.Status, Is.EqualTo(HumanSolverStatus.Solved));
            Assert.That(unique.Status, Is.EqualTo(UniquenessStatus.Unique));
            Assert.That(unique.Solutions.Count, Is.EqualTo(1));
            Assert.That(unique.Solutions[0], Is.EquivalentTo(puzzle.SolutionPaws));
            Assert.That(human.Assignments.Where(a => a.State == CellKnowledge.Paw).Select(a => a.Coordinate),
                Is.EquivalentTo(puzzle.SolutionPaws));
        }

        [Test]
        public void FixtureAndManualReplayAreFreshAndDeterministic()
        {
            var first = new TutorialController();
            ReachPractice(first);
            Assert.That(first.Tap(C(0, 0)), Is.True);
            var second = new TutorialController();
            Assert.That(second.Session, Is.Not.SameAs(first.Session));
            Assert.That(second.Session.Puzzle, Is.Not.SameAs(first.Session.Puzzle));
            Assert.That(second.Session.Puzzle.Seed, Is.EqualTo(first.Session.Puzzle.Seed));
            Assert.That(second.Session.Puzzle.SolutionPaws, Is.EqualTo(first.Session.Puzzle.SolutionPaws));
            Assert.That(second.Session.Puzzle.Arrows, Is.EquivalentTo(first.Session.Puzzle.Arrows));
            Assert.That(second.Step, Is.EqualTo(TutorialStep.Welcome));
            Assert.That(second.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            ReachPractice(second);
            Assert.That(second.EndGesture(), Is.False, "A previous replay must not carry practice progress.");
        }

        [Test]
        public void InformationalStepsRejectInputAndOnlyContinueAdvances()
        {
            var controller = new TutorialController();
            foreach (var expected in new[] { TutorialStep.Welcome, TutorialStep.Goal, TutorialStep.Begin, TutorialStep.StatesIntro, TutorialStep.EmptyState, TutorialStep.XState, TutorialStep.PawState, TutorialStep.HatchedCells, TutorialStep.Arrows })
            {
                Assert.That(controller.Step, Is.EqualTo(expected));
                Assert.That(controller.CanContinue, Is.True);
                Assert.That(controller.Tap(C(0, 0)), Is.False);
                Assert.That(controller.ApplyDrag(new[] { C(0, 0) }), Is.Zero);
                Assert.That(controller.EndGesture(), Is.False);
                Assert.That(controller.Continue(), Is.True);
            }
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Practice));
            Assert.That(controller.CanContinue, Is.False);
            Assert.That(controller.Continue(), Is.False);
            Assert.That(controller.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void PracticeRequiresActualThreeTapCycleAndDoesNotAcceptDraggingOrOffTargetInput()
        {
            var controller = new TutorialController();
            ReachPractice(controller);
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(C(0, 1)), Is.False);
            Assert.That(controller.Tap(C(-1, 0)), Is.False);
            Assert.That(controller.ApplyDrag(new[] { C(0, 0) }), Is.Zero);
            Assert.That(controller.CanDrag(C(0, 0)), Is.False);
            Assert.That(controller.Tap(C(0, 0)), Is.True);
            Assert.That(controller.Session.GetMark(C(0, 0)), Is.EqualTo(PlayerMark.X));
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(C(0, 0)), Is.True);
            Assert.That(controller.Session.GetMark(C(0, 0)), Is.EqualTo(PlayerMark.Paw));
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(C(0, 0)), Is.True);
            Assert.That(controller.Session.GetMark(C(0, 0)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Practice));
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.ZeroColumn));
            Assert.That(controller.EndGesture(), Is.False);
        }

        [Test]
        public void DragCannotAdvanceMidGestureOrLeakIntoNextLesson()
        {
            var controller = new TutorialController();
            ReachZeroColumn(controller);
            var swept = new[] { C(0, 0), C(1, 0), C(2, 0), C(3, 0), C(4, 0), C(4, 0), C(0, 1), C(0, 4), C(5, 0) };
            Assert.That(controller.ApplyDrag(swept), Is.EqualTo(4));
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.ZeroColumn));
            Assert.That(controller.Session.GetMark(C(0, 1)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(controller.Session.GetMark(C(0, 4)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.ZeroRow));
            Assert.That(controller.EndGesture(), Is.False, "One end/cancel cannot consume the next instruction.");
            Assert.That(controller.ApplyDrag(new[] { C(0, 0), C(0, 1), C(0, 2), C(0, 3), C(0, 4) }), Is.EqualTo(2));
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.FirstPaw));
            Assert.That(controller.Session.Marks.ContainsKey(C(0, 2)), Is.False);
            Assert.That(controller.Session.Marks.ContainsKey(C(0, 3)), Is.False);
        }

        [Test]
        public void PartialGestureDoesNotAdvanceAndTapsCanSatisfyAnXLesson()
        {
            var controller = new TutorialController();
            ReachZeroColumn(controller);
            Assert.That(controller.ApplyDrag(null), Is.Zero);
            Assert.That(controller.ApplyDrag(Array.Empty<BoardCoordinate>()), Is.Zero);
            Assert.That(controller.ApplyDrag(new[] { C(0, 0) }), Is.EqualTo(1));
            Assert.That(controller.EndGesture(), Is.False);
            foreach (var cell in new[] { C(2, 0), C(3, 0), C(4, 0) })
                Assert.That(controller.Tap(cell), Is.True);
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.ZeroRow));
        }

        [Test]
        public void PawLessonUsesOrdinaryTwoTapsAndCanRecoverFromAnExtraTapBeforeRelease()
        {
            var controller = new TutorialController();
            ReachFirstPaw(controller);
            Assert.That(controller.ApplyDrag(new[] { C(1, 4) }), Is.Zero);
            Assert.That(controller.Tap(C(2, 1)), Is.False);
            Assert.That(controller.Tap(C(1, 4)), Is.True);
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(C(1, 4)), Is.True);
            Assert.That(controller.Tap(C(1, 4)), Is.True); // Paw -> Empty before release.
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(C(1, 4)), Is.True);
            Assert.That(controller.Tap(C(1, 4)), Is.True);
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.FirstColumnX));
        }

        [Test]
        public void DiagonalPawRequiresTheFocusedArrowHoldAfterItsIntro()
        {
            var controller = new TutorialController();
            ReachArrowHoldIntro(controller);
            var arrow = C(0, 3);
            var paw = C(2, 1);
            Assert.That(controller.CanContinue, Is.True);
            Assert.That(controller.HighlightedCells, Is.EqualTo(new[] { arrow }));
            Assert.That(controller.CanHoldArrow(arrow), Is.False);
            Assert.That(controller.RecordArrowHold(arrow), Is.False);

            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.DiagonalPaw));
            Assert.That(controller.CanContinue, Is.False);
            Assert.That(controller.HighlightedCells, Is.EqualTo(new[] { arrow }));
            Assert.That(controller.CanHoldArrow(arrow), Is.True);
            Assert.That(controller.CanHoldArrow(C(1, 3)), Is.False);
            Assert.That(controller.CanHoldArrow(C(3, 2)), Is.False);
            Assert.That(controller.CanInteract(paw), Is.False);
            Assert.That(controller.Tap(paw), Is.False);
            Assert.That(controller.RecordArrowHold(C(1, 3)), Is.False);
            Assert.That(controller.RecordArrowHold(arrow), Is.True);
            Assert.That(controller.CanInteract(paw), Is.True);
            PlacePaw(controller, paw, TutorialStep.DiagonalPaw, TutorialStep.FourthPaw);
        }

        [Test]
        public void CompleteScriptedReplayResetsForIndependentPracticeThenWinsNormally()
        {
            var controller = new TutorialController();
            var guided = ReachIndependent(controller);
            Assert.That(guided.IsCompleted, Is.True);
            Assert.That(controller.Session, Is.Not.SameAs(guided));
            Assert.That(controller.Session.IsCompleted, Is.False);
            Assert.That(controller.IsComplete, Is.False);
            Assert.That(controller.Session.Puzzle.Seed, Is.EqualTo(guided.Puzzle.Seed));
            Assert.That(controller.Session.Puzzle.SolutionPaws, Is.EqualTo(guided.Puzzle.SolutionPaws));
            Assert.That(controller.Session.Puzzle.Arrows, Is.EquivalentTo(guided.Puzzle.Arrows));
            Assert.That(controller.Session.ProtectedCells, Is.EqualTo(guided.ProtectedCells));
            Assert.That(controller.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(controller.MessageKey, Is.EqualTo("tutorial.independent"));
            Assert.That(controller.HighlightedCells, Is.Empty);
            Assert.That(controller.HighlightedRows, Is.Empty);
            Assert.That(controller.HighlightedColumns, Is.Empty);
            Assert.That(controller.DemoCoordinate, Is.Null);
            Assert.That(controller.DemoMark, Is.Null);
            Assert.That(controller.EndGesture(), Is.False);
            foreach (var cell in controller.Session.Puzzle.SolutionPaws)
            {
                Assert.That(controller.CanInteract(cell), Is.True);
                Assert.That(controller.CanDrag(cell), Is.True);
                Assert.That(controller.Tap(cell), Is.True);
                Assert.That(controller.EndGesture(), Is.False);
                Assert.That(controller.Tap(cell), Is.True);
                if (!controller.Session.IsCompleted)
                    Assert.That(controller.EndGesture(), Is.False);
            }
            Assert.That(controller.Session.IsCompleted, Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Independent), "Win locks marks immediately; lesson transition waits for release.");
            Assert.That(controller.EndGesture(), Is.True);
            AssertCompletedReadOnly(controller);
        }

        [Test]
        public void IndependentBoardAllowsOrdinaryDragAndWrongPawsBlockCompletionUntilCorrected()
        {
            var controller = new TutorialController();
            ReachIndependent(controller);
            var extra = C(0, 0);
            Assert.That(controller.ApplyDrag(new[] { extra, extra, C(1, 0), C(0, 3), C(-1, 0) }), Is.EqualTo(1));
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(extra), Is.True); // X -> extra Paw.
            foreach (var cell in controller.Session.Puzzle.SolutionPaws)
            {
                Assert.That(controller.Tap(cell), Is.True);
                Assert.That(controller.Tap(cell), Is.True);
            }
            Assert.That(controller.Session.IsCompleted, Is.False);
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Independent));
            Assert.That(controller.Tap(extra), Is.True); // Paw -> Empty restores exact paw set.
            Assert.That(controller.Session.IsCompleted, Is.True);
            Assert.That(controller.EndGesture(), Is.True);
            AssertCompletedReadOnly(controller);
        }

        [Test]
        public void StateDemonstrationsArePresentationOnlyAndFixedKindsHaveSeparateHighlights()
        {
            var controller = new TutorialController();
            for (var i = 0; i < 3; i++) Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.StatesIntro));
            Assert.That(controller.DemoMark, Is.Null);
            Assert.That(controller.Continue(), Is.True);
            foreach (var expected in new[] { PlayerMark.Empty, PlayerMark.X, PlayerMark.Paw })
            {
                var coordinate = expected == PlayerMark.Paw ? C(2, 1) : C(0, 0);
                Assert.That(controller.DemoCoordinate, Is.EqualTo(coordinate));
                Assert.That(controller.DemoMark, Is.EqualTo(expected));
                Assert.That(controller.HighlightedCells, Is.EqualTo(new[] { coordinate }));
                if (expected != PlayerMark.Empty)
                    Assert.That(controller.Session.Puzzle.IsSolutionPaw(coordinate), Is.EqualTo(expected == PlayerMark.Paw));
                Assert.That(controller.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
                Assert.That(controller.Session.IsCompleted, Is.False);
                Assert.That(controller.CanInteract(C(0, 0)), Is.False);
                Assert.That(controller.Tap(C(0, 0)), Is.False);
                Assert.That(controller.Continue(), Is.True);
            }
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.HatchedCells));
            Assert.That(controller.DemoCoordinate, Is.Null);
            Assert.That(controller.DemoMark, Is.Null);
            Assert.That(controller.HighlightedCells, Is.EquivalentTo(controller.Session.ProtectedCells));
            Assert.That(controller.HighlightedCells.Intersect(controller.Session.Puzzle.Arrows.Keys), Is.Empty);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Arrows));
            Assert.That(controller.HighlightedCells, Is.EquivalentTo(controller.Session.Puzzle.Arrows.Keys));
            Assert.That(controller.HighlightedCells.Intersect(controller.Session.ProtectedCells), Is.Empty);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Practice));
            Assert.That(controller.Session.GetMark(C(0, 0)), Is.EqualTo(PlayerMark.Empty));
            Assert.That(controller.EndGesture(), Is.False, "Presentation cannot count as practice input.");
        }

        [Test]
        public void HighlightViewsCannotBeMutatedAndProtectionNeverBecomesAnInputTarget()
        {
            var controller = new TutorialController();
            ReachPractice(controller);
            Assert.Throws<NotSupportedException>(() => ((ICollection<BoardCoordinate>)controller.Targets).Add(C(0, 1)));
            Assert.Throws<NotSupportedException>(() => ((ICollection<BoardCoordinate>)controller.HighlightedCells).Clear());
            ReachZeroColumnFromPractice(controller);
            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)controller.HighlightedColumns).Add(1));
            Assert.That(controller.Targets, Has.None.EqualTo(C(1, 0)));
            foreach (var fixedCell in controller.Session.ProtectedCells.Concat(controller.Session.Puzzle.Arrows.Keys))
            {
                Assert.That(controller.CanTap(fixedCell), Is.False);
                Assert.That(controller.CanDrag(fixedCell), Is.False);
                Assert.That(controller.Tap(fixedCell), Is.False);
            }
        }

        private static GameSession ReachIndependent(TutorialController controller)
        {
            ReachArrowHoldIntro(controller);
            Assert.That(controller.Session.GetMark(C(4, 2)), Is.EqualTo(PlayerMark.Paw), "Swipe must preserve the paw it crosses.");
            Assert.That(controller.HighlightedCells, Is.EqualTo(new[] { C(0, 3) }));
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.DiagonalPaw));
            Assert.That(controller.RecordArrowHold(C(0, 3)), Is.True);
            PlacePaw(controller, C(2, 1), TutorialStep.DiagonalPaw, TutorialStep.FourthPaw);
            PlacePaw(controller, C(2, 3), TutorialStep.FourthPaw, TutorialStep.SingleX);
            Assert.That(controller.Tap(C(3, 3)), Is.True);
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.FinalPaw));
            Assert.That(controller.Tap(C(3, 1)), Is.True);
            Assert.That(controller.Session.IsCompleted, Is.False);
            Assert.That(controller.Tap(C(3, 1)), Is.True);
            Assert.That(controller.Session.IsCompleted, Is.True, "Guided exact paws must also win immediately before release.");
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.FinalPaw));
            var guided = controller.Session;
            Assert.That(controller.CanInteract(C(3, 1)), Is.False);
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Independent));
            return guided;
        }

        private static void ReachArrowHoldIntro(TutorialController controller)
        {
            ReachFirstPaw(controller);
            PlacePaw(controller, C(1, 4), TutorialStep.FirstPaw, TutorialStep.FirstColumnX);
            Drag(controller, new[] { C(0, 4), C(1, 4), C(2, 4), C(3, 4), C(4, 4) }, 3, TutorialStep.SecondPaw);
            PlacePaw(controller, C(4, 2), TutorialStep.SecondPaw, TutorialStep.SecondRowX);
            Drag(controller, new[] { C(4, 0), C(4, 1), C(4, 2), C(4, 3), C(4, 4) }, 2, TutorialStep.ArrowHoldIntro);
        }

        private static void AssertCompletedReadOnly(TutorialController controller)
        {
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Complete));
            Assert.That(controller.IsComplete, Is.True);
            var marks = controller.Session.Marks.ToArray();
            Assert.That(controller.Tap(C(3, 1)), Is.False);
            Assert.That(controller.ApplyDrag(new[] { C(3, 1) }), Is.Zero);
            Assert.That(controller.Session.Tap(C(3, 1)), Is.False);
            Assert.That(controller.Session.Clear(), Is.False);
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Continue(), Is.False);
            Assert.That(controller.Session.Marks, Is.EquivalentTo(marks));
            Assert.That(controller.Session.Marks.Where(p => p.Value == PlayerMark.Paw).Select(p => p.Key),
                Is.EquivalentTo(controller.Session.Puzzle.SolutionPaws));
        }

        private static void ReachPractice(TutorialController controller)
        {
            for (var i = 0; i < 9; i++) Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(TutorialStep.Practice));
        }

        private static void ReachZeroColumnFromPractice(TutorialController controller)
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.That(controller.Tap(C(0, 0)), Is.True);
                Assert.That(controller.EndGesture(), Is.EqualTo(i == 2));
            }
        }

        private static void ReachZeroColumn(TutorialController controller)
        {
            ReachPractice(controller);
            ReachZeroColumnFromPractice(controller);
        }

        private static void ReachFirstPaw(TutorialController controller)
        {
            ReachZeroColumn(controller);
            Drag(controller, new[] { C(0, 0), C(1, 0), C(2, 0), C(3, 0), C(4, 0) }, 4, TutorialStep.ZeroRow);
            Drag(controller, new[] { C(0, 0), C(0, 1), C(0, 2), C(0, 3), C(0, 4) }, 2, TutorialStep.FirstPaw);
        }

        private static void Drag(TutorialController controller, BoardCoordinate[] cells, int expectedChanges, TutorialStep next)
        {
            var previous = controller.Step;
            Assert.That(controller.ApplyDrag(cells), Is.EqualTo(expectedChanges));
            Assert.That(controller.Step, Is.EqualTo(previous));
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(next));
            Assert.That(controller.Session.IsCompleted, Is.False);
        }

        private static void PlacePaw(TutorialController controller, BoardCoordinate cell, TutorialStep current, TutorialStep next)
        {
            Assert.That(controller.Step, Is.EqualTo(current));
            Assert.That(controller.Tap(cell), Is.True);
            Assert.That(controller.Session.GetMark(cell), Is.EqualTo(PlayerMark.X));
            Assert.That(controller.EndGesture(), Is.False);
            Assert.That(controller.Tap(cell), Is.True);
            Assert.That(controller.Session.GetMark(cell), Is.EqualTo(PlayerMark.Paw));
            Assert.That(controller.Session.IsCompleted, Is.False);
            Assert.That(controller.Step, Is.EqualTo(current));
            Assert.That(controller.EndGesture(), Is.True);
            Assert.That(controller.Step, Is.EqualTo(next));
        }
    }
}
