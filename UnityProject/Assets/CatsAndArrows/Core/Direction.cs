using System;
using System.Collections.Generic;

namespace CatsAndArrows.Core
{
    public enum Direction
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        UpLeft = 4,
        UpRight = 5,
        DownLeft = 6,
        DownRight = 7
    }

    // Board rows increase downwards. Rays exclude their origin, continue through
    // all cell kinds, and stop at the first board edge without wrapping.
    public static class DirectionGeometry
    {
        public static bool IsCardinal(Direction direction) => direction == Direction.Up ||
            direction == Direction.Down || direction == Direction.Left || direction == Direction.Right;

        public static IEnumerable<BoardCoordinate> GetDirectionalSegment(int size, BoardCoordinate origin, Direction direction)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (origin.Row < 0 || origin.Row >= size || origin.Column < 0 || origin.Column >= size)
                throw new ArgumentOutOfRangeException(nameof(origin));
            int rowStep;
            int columnStep;
            switch (direction)
            {
                case Direction.Up: rowStep = -1; columnStep = 0; break;
                case Direction.Down: rowStep = 1; columnStep = 0; break;
                case Direction.Left: rowStep = 0; columnStep = -1; break;
                case Direction.Right: rowStep = 0; columnStep = 1; break;
                case Direction.UpLeft: rowStep = -1; columnStep = -1; break;
                case Direction.UpRight: rowStep = -1; columnStep = 1; break;
                case Direction.DownLeft: rowStep = 1; columnStep = -1; break;
                case Direction.DownRight: rowStep = 1; columnStep = 1; break;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            var row = origin.Row + rowStep;
            var column = origin.Column + columnStep;
            while (row >= 0 && row < size && column >= 0 && column < size)
            {
                yield return new BoardCoordinate(row, column);
                row += rowStep;
                column += columnStep;
            }
        }
    }
}
