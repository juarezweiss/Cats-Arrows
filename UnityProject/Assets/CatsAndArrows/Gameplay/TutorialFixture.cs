using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    // Issue #26: a deliberately authored lesson; ordinary boards remain seed-generated.
    public static class TutorialFixture
    {
        public const int Seed = 26;
        public const string Version = "tutorial-v1";

        public static PuzzleDefinition CreatePuzzle() => new PuzzleDefinition(
            5, Seed, Version,
            new[]
            {
                new BoardCoordinate(1, 4), new BoardCoordinate(4, 2),
                new BoardCoordinate(2, 1), new BoardCoordinate(2, 3), new BoardCoordinate(3, 1)
            },
            new[]
            {
                new ArrowClue(new BoardCoordinate(1, 3), Direction.Right),
                new ArrowClue(new BoardCoordinate(3, 2), Direction.Down),
                new ArrowClue(new BoardCoordinate(0, 3), Direction.DownLeft)
            });

        public static GameSession CreateSession()
        {
            var puzzle = CreatePuzzle();
            return new GameSession(puzzle, ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle)));
        }
    }
}
