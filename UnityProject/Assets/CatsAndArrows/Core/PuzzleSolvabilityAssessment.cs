using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public enum PuzzleAssessmentStatus
    {
        Accepted, InvalidCandidate, ProfileRejected, HumanRejected, UniquenessRejected, IntendedSolutionMismatch
    }

    public sealed class PuzzleAssessmentResult
    {
        public PuzzleAssessmentStatus Status { get; }
        public bool IsAccepted => Status == PuzzleAssessmentStatus.Accepted;
        public SolverResult Solver { get; }
        public IReadOnlyList<string> Errors { get; }
        internal PuzzleAssessmentResult(PuzzleAssessmentStatus status, SolverResult solver, IEnumerable<string> errors)
        { Status = status; Solver = solver; Errors = Array.AsReadOnly(errors.ToArray()); }
    }

    // Admission is separate from solving: profile restrictions and the intended answer
    // are never extra constraints supplied to either engine.
    public static class PuzzleSolvabilityAssessment
    {
        public static PuzzleAssessmentResult Assess(PuzzleDefinition puzzle, DifficultyProfile profile, SolverOptions options = null)
        {
            if (puzzle == null || profile == null)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.InvalidCandidate, null, new[] { "Puzzle and profile are required." });
            var clues = PuzzleClues.FromPuzzle(puzzle);
            var solver = new PuzzleSolver().Solve(clues, options);
            if (solver.Human.Status == HumanSolverStatus.InvalidInput)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.InvalidCandidate, solver, solver.Human.Errors);

            var candidateErrors = new List<string>();
            foreach (var cat in puzzle.SolutionCats)
            {
                if (!puzzle.IsInside(cat)) candidateErrors.Add("Intended cat is outside the board.");
                if (puzzle.IsArrow(cat)) candidateErrors.Add("An arrow cannot be an intended cat.");
            }
            foreach (var arrow in puzzle.Arrows)
                if (!puzzle.GetDirectionalSegment(arrow.Key, arrow.Value).Any(puzzle.IsSolutionCat))
                    candidateErrors.Add("An arrow is false for the intended answer.");
            if (candidateErrors.Count > 0)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.InvalidCandidate, solver, candidateErrors);

            if (profile.MaxZeroRows < 0 || profile.MaxZeroColumns < 0)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.ProfileRejected, solver, new[] { "Profile zero-line limits cannot be negative." });
            var profileValidation = PuzzleValidator.Validate(puzzle, profile);
            if (!profileValidation.IsValid)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.ProfileRejected, solver, profileValidation.Errors);
            if (!solver.SolvedWithoutGuessing)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.HumanRejected, solver, new[] { "Approved human deductions did not complete the board." });
            if (!solver.HasUniqueSolution)
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.UniquenessRejected, solver, new[] { "Exactly one solution has not been proven." });

            var humanCats = solver.Human.Assignments.Where(a => a.State == CellKnowledge.Cat).Select(a => a.Coordinate);
            var verifiedCats = solver.Uniqueness.Solutions[0];
            var intended = new HashSet<BoardCoordinate>(puzzle.SolutionCats);
            if (!intended.SetEquals(humanCats) || !intended.SetEquals(verifiedCats))
                return new PuzzleAssessmentResult(PuzzleAssessmentStatus.IntendedSolutionMismatch, solver, new[] { "Derived answers differ from the intended answer." });
            return new PuzzleAssessmentResult(PuzzleAssessmentStatus.Accepted, solver, Array.Empty<string>());
        }
    }
}
