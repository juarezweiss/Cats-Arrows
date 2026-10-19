using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public sealed class PuzzleDefinition
    {
        private readonly HashSet<BoardCoordinate> _solutionPaws;
        private readonly Dictionary<BoardCoordinate, Direction> _arrows;
        private readonly IReadOnlyCollection<BoardCoordinate> _solutionPawsView;
        private readonly IReadOnlyDictionary<BoardCoordinate, Direction> _arrowsView;

        public int Size { get; }
        public int Seed { get; }
        public string GeneratorVersion { get; }
        public IReadOnlyCollection<BoardCoordinate> SolutionPaws => _solutionPawsView;
        public IReadOnlyDictionary<BoardCoordinate, Direction> Arrows => _arrowsView;

        public PuzzleDefinition(
            int size,
            int seed,
            string generatorVersion,
            IEnumerable<BoardCoordinate> solutionPaws,
            IEnumerable<ArrowClue> arrows)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (string.IsNullOrWhiteSpace(generatorVersion)) throw new ArgumentException("Generator version is required.", nameof(generatorVersion));

            Size = size;
            Seed = seed;
            GeneratorVersion = generatorVersion;
            _solutionPaws = new HashSet<BoardCoordinate>(solutionPaws ?? throw new ArgumentNullException(nameof(solutionPaws)));
            _arrows = (arrows ?? throw new ArgumentNullException(nameof(arrows)))
                .ToDictionary(a => a.Coordinate, a => a.Direction);
            _solutionPawsView = Array.AsReadOnly(_solutionPaws.OrderBy(c => c.Row).ThenBy(c => c.Column).ToArray());
            _arrowsView = new System.Collections.ObjectModel.ReadOnlyDictionary<BoardCoordinate, Direction>(_arrows);
        }

        public bool IsInside(BoardCoordinate coordinate) =>
            coordinate.Row >= 0 && coordinate.Row < Size && coordinate.Column >= 0 && coordinate.Column < Size;

        public bool IsArrow(BoardCoordinate coordinate) => _arrows.ContainsKey(coordinate);
        public bool IsSolutionPaw(BoardCoordinate coordinate) => _solutionPaws.Contains(coordinate);

        public int GetRowPawCount(int row) => _solutionPaws.Count(c => c.Row == row);
        public int GetColumnPawCount(int column) => _solutionPaws.Count(c => c.Column == column);

        public IEnumerable<BoardCoordinate> GetDirectionalSegment(BoardCoordinate origin, Direction direction) =>
            DirectionGeometry.GetDirectionalSegment(Size, origin, direction);
    }
}
