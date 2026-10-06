using System.Collections.Generic;

namespace CatsAndArrows.Core
{
    public enum DeductionKind
    {
        ZeroLine,
        LineSatisfied,
        FillRemainingLine,
        ArrowSegment,
        ConstraintPropagation
    }

    public sealed class DeductionStep
    {
        public DeductionKind Kind { get; }
        public string Explanation { get; }
        public IReadOnlyCollection<BoardCoordinate> AffectedCells { get; }

        public DeductionStep(DeductionKind kind, string explanation, IReadOnlyCollection<BoardCoordinate> affectedCells)
        {
            Kind = kind;
            Explanation = explanation;
            AffectedCells = affectedCells;
        }
    }

    public sealed class SolverResult
    {
        public bool SolvedWithoutGuessing { get; }
        public bool HasUniqueSolution { get; }
        public IReadOnlyList<DeductionStep> Steps { get; }

        public SolverResult(bool solvedWithoutGuessing, bool hasUniqueSolution, IReadOnlyList<DeductionStep> steps)
        {
            SolvedWithoutGuessing = solvedWithoutGuessing;
            HasUniqueSolution = hasUniqueSolution;
            Steps = steps;
        }
    }

    public interface IPuzzleSolver
    {
        SolverResult Solve(PuzzleDefinition puzzle);
    }
}
