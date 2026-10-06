using System;

namespace CatsAndArrows.Core
{
    public readonly struct BoardCoordinate : IEquatable<BoardCoordinate>
    {
        public int Row { get; }
        public int Column { get; }

        public BoardCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public bool Equals(BoardCoordinate other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is BoardCoordinate other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Column;
        public override string ToString() => $"({Row},{Column})";

        public static bool operator ==(BoardCoordinate left, BoardCoordinate right) => left.Equals(right);
        public static bool operator !=(BoardCoordinate left, BoardCoordinate right) => !left.Equals(right);
    }
}
