using System;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    // Session orchestration without presentation, preferences, or Unity objects.
    public sealed class EasyGameLoop
    {
        private readonly Func<int, GenerationResult> _generate;
        private int _nextSeed;
        private readonly DifficultySelection _difficulty;
        private bool _generating;

        public GameSession Session { get; private set; }
        public GenerationStatus? LastGenerationStatus { get; private set; }
        public int? RequestedSeed { get; private set; }
        public DifficultyProfile RequestedDifficulty { get; private set; }
        public DifficultySelection DifficultySelection => _difficulty;
        public DifficultyProfile SessionDifficulty { get; private set; }

        public EasyGameLoop(int initialSeed, Func<int, GenerationResult> generate = null, IPuzzleGenerator generator = null, DifficultySelection difficultySelection = null)
        {
            _nextSeed = initialSeed;
            _difficulty = difficultySelection ?? DifficultySelection.CreateSeeded(initialSeed);
            var service = generator ?? new SeededPuzzleGenerator();
            _generate = generate ?? (seed => service.Generate(RequestedDifficulty, seed));
        }

        public bool NewGame()
        {
            if (_generating) return false;
            _generating = true;
            try
            {
                RequestedDifficulty = _difficulty.Choose();
                RequestedSeed = _nextSeed;
                _nextSeed = unchecked(_nextSeed + 1);
                var result = _generate(RequestedSeed.Value);
                LastGenerationStatus = result?.Status ?? GenerationStatus.Exhausted;
                Session = null;
                SessionDifficulty = null;
                if (result == null || !result.IsSuccess || result.Puzzle == null || result.Assessment == null ||
                    !result.Assessment.IsAccepted || !PuzzleValidator.Validate(result.Puzzle, RequestedDifficulty).IsValid)
                {
                    if (LastGenerationStatus == GenerationStatus.Success)
                        LastGenerationStatus = GenerationStatus.Exhausted;
                    return false;
                }

                var admitted = new GameSession(result.Puzzle, ClueProtection.Derive(PuzzleClues.FromPuzzle(result.Puzzle), RequestedDifficulty));
                if (!_difficulty.CommitStart())
                {
                    LastGenerationStatus = GenerationStatus.Exhausted;
                    return false;
                }
                Session = admitted;
                SessionDifficulty = RequestedDifficulty;
                return true;
            }
            finally { _generating = false; }
        }
    }
}
