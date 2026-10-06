using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public sealed class PuzzleDefinition
    {
        private readonly HashSet<BoardCoordinate> _solutionCats;
        private readonly Dictionary<BoardCoordinate, Direction> _arrows;

        public int Size { get; }
        public int Seed { get; }
        public string GeneratorVersion { get; }
        public IReadOnlyCollection<BoardCoordinate> SolutionCats => _solutionCats;
        public IReadOnlyDictionary<BoardCoordinate, Direction> Arrows => _arrows;

        public PuzzleDefinition(
            int size,
            int seed,
            string generatorVersion,
            IEnumerable<BoardCoordinate> solutionCats,
            IEnumerable<ArrowClue> arrows)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (string.IsNullOrWhiteSpace(generatorVersion)) throw new ArgumentException("Generator version is required.", nameof(generatorVersion));

            Size = size;
            Seed = seed;
            GeneratorVersion = generatorVersion;
            _solutionCats = new HashSet<BoardCoordinate>(solutionCats ?? throw new ArgumentNullException(nameof(solutionCats)));
            _arrows = (arrows ?? throw new ArgumentNullException(nameof(arrows)))
                .ToDictionary(a => a.Coordinate, a => a.Direction);
        }

        public bool IsInside(BoardCoordinate coordinate) =>
            coordinate.Row >= 0 && coordinate.Row < Size && coordinate.Column >= 0 && coordinate.Column < Size;

        public bool IsArrow(BoardCoordinate coordinate) => _arrows.ContainsKey(coordinate);
        public bool IsSolutionCat(BoardCoordinate coordinate) => _solutionCats.Contains(coordinate);

        public int GetRowCatCount(int row) => _solutionCats.Count(c => c.Row == row);
        public int GetColumnCatCount(int column) => _solutionCats.Count(c => c.Column == column);

        public IEnumerable<BoardCoordinate> GetDirectionalSegment(BoardCoordinate origin, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    for (var row = origin.Row - 1; row >= 0; row--) yield return new BoardCoordinate(row, origin.Column);
                    break;
                case Direction.Down:
                    for (var row = origin.Row + 1; row < Size; row++) yield return new BoardCoordinate(row, origin.Column);
                    break;
                case Direction.Left:
                    for (var column = origin.Column - 1; column >= 0; column--) yield return new BoardCoordinate(origin.Row, column);
                    break;
                case Direction.Right:
                    for (var column = origin.Column + 1; column < Size; column++) yield return new BoardCoordinate(origin.Row, column);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
