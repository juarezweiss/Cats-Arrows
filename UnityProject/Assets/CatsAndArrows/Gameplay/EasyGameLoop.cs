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
        public DifficultyProfile SelectedDifficulty { get; private set; } = DifficultyProfile.Easy;
        public DifficultyProfile SessionDifficulty { get; private set; }

        public EasyGameLoop(int initialSeed, Func<int, GenerationResult> generate = null, IPuzzleGenerator generator = null)
        {
            _nextSeed = initialSeed;
            var service = generator ?? new SeededPuzzleGenerator();
            _generate = generate ?? (seed => service.Generate(SelectedDifficulty, seed));
        }

        // Menu selection affects the next board, not the live session being reviewed.
        public void SelectDifficulty(string id)
        {
            SelectedDifficulty = id switch
            {
                "easy" => DifficultyProfile.Easy,
                "medium" => DifficultyProfile.Medium,
                "hard" => DifficultyProfile.Hard,
                _ => throw new ArgumentException("Unsupported difficulty.", nameof(id))
            };
        }

        public bool NewGame()
        {
            RequestedSeed = _nextSeed;
            _nextSeed = unchecked(_nextSeed + 1);
            var result = _generate(RequestedSeed.Value);
            LastGenerationStatus = result?.Status ?? GenerationStatus.Exhausted;
            Session = null;
            SessionDifficulty = null;
            if (result == null || !result.IsSuccess || result.Puzzle == null || result.Assessment == null ||
                !result.Assessment.IsAccepted || !PuzzleValidator.Validate(result.Puzzle, SelectedDifficulty).IsValid)
            {
                if (LastGenerationStatus == GenerationStatus.Success)
                    LastGenerationStatus = GenerationStatus.Exhausted;
                return false;
            }

            Session = new GameSession(result.Puzzle, ClueProtection.Derive(PuzzleClues.FromPuzzle(result.Puzzle)));
            SessionDifficulty = SelectedDifficulty;
            return true;
        }
    }
}
