using System.Collections.Generic;

namespace CatsAndArrows.Core
{
    public sealed class PuzzleValidationResult
    {
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;

        public PuzzleValidationResult(IReadOnlyList<string> errors)
        {
            Errors = errors;
        }
    }
}
