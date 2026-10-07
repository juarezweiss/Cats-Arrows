using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public enum CellKnowledge { Unknown, Cat, NotCat }
    public enum ConstraintKind { Row, Column, Arrow }
    public enum HumanSolverStatus { Solved, Stalled, Contradiction, InvalidInput, BudgetExceeded }
    public enum UniquenessStatus { Unique, Multiple, NoSolution, InvalidInput, Incomplete }
    public enum DeductionKind
    {
        ZeroLine,
        LineSatisfied,
        FillRemainingLine,
        ArrowSegment,
        ConstraintPropagation,
        ArrowLineExclusion
    }

    public static class DeductionRules
    {
        public const string RulesetVersion = "human-v1";
        public const string ZeroLine = "zero-line";
        public const string LineSatisfied = "line-satisfied";
        public const string FillRemainingLine = "fill-remaining-line";
        public const string ArrowSingleton = "arrow-singleton";
        public const string ArrowLineExclusion = "arrow-line-exclusion";
    }

    public readonly struct CellAssignment
    {
        public BoardCoordinate Coordinate { get; }
        public CellKnowledge State { get; }
        public CellAssignment(BoardCoordinate coordinate, CellKnowledge state) { Coordinate = coordinate; State = state; }
    }

    public readonly struct ConstraintReference
    {
        public ConstraintKind Kind { get; }
        public int Index { get; }
        public BoardCoordinate Coordinate { get; }
        public Direction Direction { get; }
        public ConstraintReference(ConstraintKind kind, int index, BoardCoordinate coordinate = default, Direction direction = default)
        { Kind = kind; Index = index; Coordinate = coordinate; Direction = direction; }
    }

    public sealed class DeductionPremise
    {
        public ConstraintReference Source { get; }
        // Arrow requirements are lower bounds; row/column totals are exact.
        public int RequiredCats { get; }
        public IReadOnlyList<CellAssignment> Cells { get; }
        public int KnownCatCount => Cells.Count(c => c.State == CellKnowledge.Cat);
        public int UnknownCount => Cells.Count(c => c.State == CellKnowledge.Unknown);
        public DeductionPremise(ConstraintReference source, int requiredCats, IEnumerable<CellAssignment> cells)
        { Source = source; RequiredCats = requiredCats; Cells = Array.AsReadOnly(cells.ToArray()); }
    }

    public sealed class DeductionStep
    {
        public DeductionKind Kind { get; }
        public int Index { get; }
        public string RuleId { get; }
        public IReadOnlyList<DeductionPremise> Premises { get; }
        public IReadOnlyList<CellAssignment> Conclusions { get; }
        public IReadOnlyList<ConstraintReference> Sources { get; }
        public IReadOnlyList<BoardCoordinate> AffectedCells { get; }
        public IReadOnlyList<int> DependencyStepIds { get; }

        public DeductionStep(int index, string ruleId, DeductionKind kind, IEnumerable<DeductionPremise> premises,
            IEnumerable<CellAssignment> conclusions, IEnumerable<int> dependencyStepIds = null)
        {
            Index = index; RuleId = ruleId; Kind = kind;
            Premises = Array.AsReadOnly(premises.ToArray());
            Conclusions = Array.AsReadOnly(conclusions.ToArray());
            Sources = Array.AsReadOnly(Premises.Select(p => p.Source).ToArray());
            AffectedCells = Array.AsReadOnly(Conclusions.Select(c => c.Coordinate).ToArray());
            DependencyStepIds = Array.AsReadOnly((dependencyStepIds ?? Array.Empty<int>()).Distinct().OrderBy(i => i).ToArray());
        }
    }

    public sealed class SolverOptions
    {
        public int MaxHumanConstraintEvaluations { get; }
        public int MaxVerifierNodes { get; }
        public SolverOptions(int maxHumanConstraintEvaluations = 100000, int maxVerifierNodes = 1000000)
        {
            if (maxHumanConstraintEvaluations < 0) throw new ArgumentOutOfRangeException(nameof(maxHumanConstraintEvaluations));
            if (maxVerifierNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxVerifierNodes));
            MaxHumanConstraintEvaluations = maxHumanConstraintEvaluations;
            MaxVerifierNodes = maxVerifierNodes;
        }
    }

    public sealed class SolverMetrics
    {
        public string RulesetVersion => DeductionRules.RulesetVersion;
        public int StepCount { get; }
        public IReadOnlyDictionary<string, int> RuleCounts { get; }
        public string HardestRuleId { get; }
        public int CatsResolved { get; }
        public int NotCatsResolved { get; }
        public int MaximumDependencyDepth { get; }
        public int MaximumUnresolvedCandidatesPerLine { get; }
        public double InitialArrowDensity { get; }
        // Fraction of non-arrow cells eliminated by the first deduction.
        public double ImmediateEliminationProportion { get; }
        public int ConstraintEvaluations { get; }
        internal SolverMetrics(IReadOnlyList<DeductionStep> steps, int size, int arrowCount, int maxCandidates, int evaluations)
        {
            StepCount = steps.Count; ConstraintEvaluations = evaluations;
            RuleCounts = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(
                steps.GroupBy(s => s.RuleId).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()));
            var rank = new[] { DeductionRules.ZeroLine, DeductionRules.LineSatisfied, DeductionRules.FillRemainingLine,
                DeductionRules.ArrowSingleton, DeductionRules.ArrowLineExclusion };
            HardestRuleId = steps.Count == 0 ? null : steps.OrderByDescending(s => Array.IndexOf(rank, s.RuleId)).First().RuleId;
            CatsResolved = steps.Sum(s => s.Conclusions.Count(c => c.State == CellKnowledge.Cat));
            NotCatsResolved = steps.Sum(s => s.Conclusions.Count(c => c.State == CellKnowledge.NotCat));
            var depths = new int[steps.Count];
            foreach (var step in steps)
                depths[step.Index] = 1 + (step.DependencyStepIds.Count == 0 ? 0 : step.DependencyStepIds.Max(i => depths[i]));
            MaximumDependencyDepth = depths.Length == 0 ? 0 : depths.Max();
            MaximumUnresolvedCandidatesPerLine = maxCandidates;
            var boardCells = (double)size * size;
            InitialArrowDensity = boardCells <= 0 ? 0 : arrowCount / boardCells;
            var normalCells = boardCells - arrowCount;
            ImmediateEliminationProportion = steps.Count == 0 || normalCells <= 0 ? 0 :
                steps[0].Conclusions.Count(c => c.State == CellKnowledge.NotCat) / normalCells;
        }
    }

    public sealed class HumanSolverResult
    {
        public HumanSolverStatus Status { get; }
        public IReadOnlyList<CellAssignment> Assignments { get; }
        public IReadOnlyList<DeductionStep> Steps { get; }
        public SolverMetrics Metrics { get; }
        public IReadOnlyList<string> Errors { get; }
        internal HumanSolverResult(HumanSolverStatus status, IEnumerable<CellAssignment> assignments,
            IEnumerable<DeductionStep> steps, SolverMetrics metrics, IEnumerable<string> errors)
        {
            Status = status; Assignments = Array.AsReadOnly(assignments.ToArray());
            Steps = Array.AsReadOnly(steps.ToArray()); Metrics = metrics; Errors = Array.AsReadOnly(errors.ToArray());
        }
    }

    public sealed class UniquenessResult
    {
        public const string VerifierVersion = "exhaustive-v1";
        public UniquenessStatus Status { get; }
        public IReadOnlyList<IReadOnlyList<BoardCoordinate>> Solutions { get; }
        public int NodesVisited { get; }
        public IReadOnlyList<string> Errors { get; }
        internal UniquenessResult(UniquenessStatus status, IEnumerable<IReadOnlyList<BoardCoordinate>> solutions,
            int nodesVisited, IEnumerable<string> errors)
        {
            Status = status; NodesVisited = nodesVisited;
            Solutions = Array.AsReadOnly(solutions.Select(s => (IReadOnlyList<BoardCoordinate>)Array.AsReadOnly(s.ToArray())).ToArray());
            Errors = Array.AsReadOnly(errors.ToArray());
        }
    }

    public sealed class SolverResult
    {
        public HumanSolverResult Human { get; }
        public UniquenessResult Uniqueness { get; }
        public bool SolvedWithoutGuessing => Human.Status == HumanSolverStatus.Solved;
        public bool HasUniqueSolution => Uniqueness.Status == UniquenessStatus.Unique;
        public IReadOnlyList<DeductionStep> Steps => Human.Steps;
        public SolverResult(HumanSolverResult human, UniquenessResult uniqueness) { Human = human; Uniqueness = uniqueness; }
    }

    public interface IPuzzleSolver
    {
        SolverResult Solve(PuzzleClues clues, SolverOptions options = null);
    }
}
