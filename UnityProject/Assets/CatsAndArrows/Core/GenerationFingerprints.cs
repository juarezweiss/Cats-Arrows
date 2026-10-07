using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CatsAndArrows.Core
{
    public static class GenerationFingerprints
    {
        public static string ForSolution(PuzzleDefinition puzzle)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            return Hash("cats-arrows-solution-v1\n" + Number(puzzle.Size) + "\n" + Cats(puzzle) + "\n");
        }

        // Logical contents only: metadata/seed/version live alongside this hash.
        public static string ForBoard(PuzzleDefinition puzzle)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            var rows = string.Join(",", Enumerable.Range(0, puzzle.Size).Select(i => Number(puzzle.GetRowCatCount(i))));
            var columns = string.Join(",", Enumerable.Range(0, puzzle.Size).Select(i => Number(puzzle.GetColumnCatCount(i))));
            var arrows = string.Join(";", puzzle.Arrows.OrderBy(a => a.Key.Row).ThenBy(a => a.Key.Column)
                .Select(a => Number(a.Key.Row) + "," + Number(a.Key.Column) + "," + Number((int)a.Value)));
            return Hash("cats-arrows-board-v1\n" + Number(puzzle.Size) + "\n" + rows + "\n" + columns + "\n" + Cats(puzzle) + "\n" + arrows + "\n");
        }

        private static string Cats(PuzzleDefinition puzzle) => string.Join(";", puzzle.SolutionCats.OrderBy(c => c.Row).ThenBy(c => c.Column)
            .Select(c => Number(c.Row) + "," + Number(c.Column)));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Hash(string contents)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(contents)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
