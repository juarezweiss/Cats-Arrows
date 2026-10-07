namespace CatsAndArrows.Core
{
    public sealed class PuzzleSolver : IPuzzleSolver
    {
        public SolverResult Solve(PuzzleClues clues, SolverOptions options = null) => new SolverResult(
            new HumanDeductionSolver().Solve(clues, options), new UniquenessVerifier().Verify(clues, options));
    }
}
