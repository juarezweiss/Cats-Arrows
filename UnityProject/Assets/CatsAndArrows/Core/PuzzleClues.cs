using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    // Detached public clues. Neither engine receives an answer or gameplay state.
    public sealed class PuzzleClues
    {
        public int Size { get; }
        public int Seed { get; }
        public string GeneratorVersion { get; }
        public IReadOnlyList<int> RowTotals { get; }
        public IReadOnlyList<int> ColumnTotals { get; }
        public IReadOnlyList<ArrowClue> Arrows { get; }
        public PuzzleClues(int size, int seed, string generatorVersion, IEnumerable<int> rowTotals,
            IEnumerable<int> columnTotals, IEnumerable<ArrowClue> arrows)
        {
            Size = size; Seed = seed; GeneratorVersion = generatorVersion;
            RowTotals = Array.AsReadOnly((rowTotals ?? throw new ArgumentNullException(nameof(rowTotals))).ToArray());
            ColumnTotals = Array.AsReadOnly((columnTotals ?? throw new ArgumentNullException(nameof(columnTotals))).ToArray());
            Arrows = Array.AsReadOnly((arrows ?? throw new ArgumentNullException(nameof(arrows)))
                .OrderBy(a => a.Coordinate.Row).ThenBy(a => a.Coordinate.Column).ThenBy(a => a.Direction).ToArray());
        }

        public static PuzzleClues FromPuzzle(PuzzleDefinition puzzle)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            return new PuzzleClues(puzzle.Size, puzzle.Seed, puzzle.GeneratorVersion,
                Enumerable.Range(0, puzzle.Size).Select(puzzle.GetRowPawCount),
                Enumerable.Range(0, puzzle.Size).Select(puzzle.GetColumnPawCount),
                puzzle.Arrows.Select(a => new ArrowClue(a.Key, a.Value)));
        }

        public bool IsInside(BoardCoordinate coordinate) => coordinate.Row >= 0 && coordinate.Row < Size &&
            coordinate.Column >= 0 && coordinate.Column < Size;

        internal IReadOnlyList<string> ValidateInput()
        {
            var errors = new List<string>();
            if (Size <= 0 || (long)Size * Size > int.MaxValue) errors.Add("Invalid board size.");
            if (string.IsNullOrWhiteSpace(GeneratorVersion)) errors.Add("Generator version is required.");
            if (RowTotals.Count != Size || ColumnTotals.Count != Size) errors.Add("Clue total lengths must equal board size.");
            if (RowTotals.Any(t => t < 0 || t > Size) || ColumnTotals.Any(t => t < 0 || t > Size))
                errors.Add("Line totals must be between zero and board size.");
            if (Arrows.Any(a => !IsInside(a.Coordinate))) errors.Add("Arrow is outside the board.");
            if (Arrows.Any(a => !Enum.IsDefined(typeof(Direction), a.Direction))) errors.Add("Invalid arrow direction.");
            if (Arrows.Select(a => a.Coordinate).Distinct().Count() != Arrows.Count) errors.Add("Duplicate arrow coordinates.");
            return errors.AsReadOnly();
        }
    }
}
