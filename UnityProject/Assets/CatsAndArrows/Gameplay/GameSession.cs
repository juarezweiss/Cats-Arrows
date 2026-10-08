using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public sealed class GameSession
    {
        private readonly Dictionary<BoardCoordinate, PlayerMark> _marks;
        private readonly HashSet<BoardCoordinate> _protectedCells;
        private readonly IReadOnlyDictionary<BoardCoordinate, PlayerMark> _marksView;
        private readonly IReadOnlyCollection<BoardCoordinate> _protectedCellsView;
        private HumanSolverResult _hintProof;

        public PuzzleDefinition Puzzle { get; }
        public bool IsCompleted { get; private set; }
        public IReadOnlyDictionary<BoardCoordinate, PlayerMark> Marks => _marksView;
        public IReadOnlyCollection<BoardCoordinate> ProtectedCells => _protectedCellsView;

        public GameSession(PuzzleDefinition puzzle, IEnumerable<BoardCoordinate> protectedCells = null)
        {
            Puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            _protectedCells = new HashSet<BoardCoordinate>(protectedCells ?? Array.Empty<BoardCoordinate>());
            foreach (var coordinate in _protectedCells)
            {
                if (!puzzle.IsInside(coordinate))
                    throw new ArgumentException("Protected cells must be inside the board.", nameof(protectedCells));
                if (puzzle.IsSolutionCat(coordinate))
                    throw new ArgumentException("Solution cat cells cannot be protected.", nameof(protectedCells));
            }
            // Arrows already have their own non-interactive cell kind.
            _protectedCells.RemoveWhere(puzzle.IsArrow);
            _protectedCellsView = Array.AsReadOnly(_protectedCells
                .OrderBy(coordinate => coordinate.Row).ThenBy(coordinate => coordinate.Column).ToArray());
            _marks = new Dictionary<BoardCoordinate, PlayerMark>();
            _marksView = new ReadOnlyDictionary<BoardCoordinate, PlayerMark>(_marks);

            for (var row = 0; row < puzzle.Size; row++)
            for (var column = 0; column < puzzle.Size; column++)
            {
                var coordinate = new BoardCoordinate(row, column);
                if (!puzzle.IsArrow(coordinate) && !_protectedCells.Contains(coordinate))
                    _marks[coordinate] = PlayerMark.Empty;
            }
            RefreshCompletion();
        }

        public bool CanInteract(BoardCoordinate coordinate) =>
            !IsCompleted && _marks.ContainsKey(coordinate);

        public bool Tap(BoardCoordinate coordinate)
        {
            if (!CanInteract(coordinate)) return false;

            _marks[coordinate] = _marks[coordinate] switch
            {
                PlayerMark.Empty => PlayerMark.X,
                PlayerMark.X => PlayerMark.Cat,
                PlayerMark.Cat => PlayerMark.Empty,
                _ => throw new ArgumentOutOfRangeException()
            };

            RefreshCompletion();
            return true;
        }

        public int ApplyDrag(IEnumerable<BoardCoordinate> coordinates)
        {
            if (IsCompleted || coordinates == null) return 0;

            var changed = 0;
            var visited = new HashSet<BoardCoordinate>();
            foreach (var coordinate in coordinates)
            {
                if (!visited.Add(coordinate)) continue;
                if (!_marks.TryGetValue(coordinate, out var mark)) continue;
                if (mark != PlayerMark.Empty) continue;
                _marks[coordinate] = PlayerMark.X;
                changed++;
            }

            RefreshCompletion();
            return changed;
        }

        public bool Clear()
        {
            if (IsCompleted) return false;
            var keys = _marks.Keys.ToArray();
            foreach (var coordinate in keys) _marks[coordinate] = PlayerMark.Empty;
            return true;
        }

        public PlayerMark GetMark(BoardCoordinate coordinate) =>
            _marks.TryGetValue(coordinate, out var mark) ? mark : PlayerMark.Empty;

        public HintResult ApplyHint()
        {
            if (IsCompleted) return new HintResult(HintStatus.Completed);

            // Player marks are hypotheses, never premises for the proof.
            _hintProof = _hintProof ?? new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(Puzzle));
            if (_hintProof.Status != HumanSolverStatus.Solved)
                return new HintResult(HintStatus.Unavailable);

            // Rescan after every request: Clear or later taps can reverse an earlier fact.
            foreach (var step in _hintProof.Steps)
            foreach (var assignment in step.Conclusions)
            {
                if (!_marks.TryGetValue(assignment.Coordinate, out var current)) continue;
                if (assignment.State == CellKnowledge.Unknown) continue;
                var target = assignment.State == CellKnowledge.Cat ? PlayerMark.Cat : PlayerMark.X;
                if (current == target) continue;

                _marks[assignment.Coordinate] = target;
                RefreshCompletion();
                return new HintResult(HintStatus.Applied, step, assignment, current != PlayerMark.Empty);
            }

            return new HintResult(HintStatus.NoChange);
        }

        private void RefreshCompletion()
        {
            var playerCats = _marks.Where(pair => pair.Value == PlayerMark.Cat).Select(pair => pair.Key);
            if (Puzzle.SolutionCats.Count == _marks.Count(pair => pair.Value == PlayerMark.Cat) &&
                new HashSet<BoardCoordinate>(playerCats).SetEquals(Puzzle.SolutionCats))
            {
                IsCompleted = true;
            }
        }
    }
}
