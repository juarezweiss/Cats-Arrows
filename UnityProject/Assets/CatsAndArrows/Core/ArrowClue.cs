namespace CatsAndArrows.Core
{
    public readonly struct ArrowClue
    {
        public BoardCoordinate Coordinate { get; }
        public Direction Direction { get; }

        public ArrowClue(BoardCoordinate coordinate, Direction direction)
        {
            Coordinate = coordinate;
            Direction = direction;
        }
    }
}
