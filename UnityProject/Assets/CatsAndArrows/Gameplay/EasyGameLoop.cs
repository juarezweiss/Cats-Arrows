using System;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    // Session orchestration without presentation, preferences, or Unity objects.
    public sealed class EasyGameLoop
    {
        private readonly Func<int, GenerationResult> _generate;
        private int _nextSeed;

        public GameSession Session { get; private set; }
        public GenerationStatus? LastGenerationStatus { get; private set; }
        public int? RequestedSeed { get; private set; }
        public DifficultyProfile SelectedDifficulty { get; } = DifficultyProfile.Easy;

        public EasyGameLoop(int initialSeed, Func<int, GenerationResult> generate = null)
        {
            _nextSeed = initialSeed;
            var generator = new SeededEasyPuzzleGenerator();
            _generate = generate ?? (seed => generator.Generate(SelectedDifficulty, seed));
        }

        public bool NewGame()
        {
            RequestedSeed = _nextSeed;
            _nextSeed = unchecked(_nextSeed + 1);
            var result = _generate(RequestedSeed.Value);
            LastGenerationStatus = result?.Status ?? GenerationStatus.Exhausted;
            Session = null;
            if (result == null || !result.IsSuccess || result.Puzzle == null || result.Assessment == null ||
                !result.Assessment.IsAccepted)
            {
                if (LastGenerationStatus == GenerationStatus.Success)
                    LastGenerationStatus = GenerationStatus.Exhausted;
                return false;
            }

            Session = new GameSession(result.Puzzle, ClueProtection.Derive(PuzzleClues.FromPuzzle(result.Puzzle)));
            return true;
        }
    }
}
