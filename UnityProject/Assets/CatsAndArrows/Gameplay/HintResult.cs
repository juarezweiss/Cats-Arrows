using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public enum HintStatus { Applied, Completed, Unavailable, NoChange }

    public sealed class HintResult
    {
        public HintStatus Status { get; }
        public DeductionStep Step { get; }
        public CellAssignment? Assignment { get; }
        public bool Corrected { get; }

        internal HintResult(HintStatus status, DeductionStep step = null,
            CellAssignment? assignment = null, bool corrected = false)
        {
            Status = status;
            Step = step;
            Assignment = assignment;
            Corrected = corrected;
        }
    }
}
