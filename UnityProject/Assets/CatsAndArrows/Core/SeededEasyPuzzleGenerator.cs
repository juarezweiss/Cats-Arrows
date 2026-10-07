using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CatsAndArrows.Core
{
    // Stateless service. All search, random streams, and work receipts belong to
    // one call; callers may prepare independently without a Unity dependency.
    public sealed class SeededEasyPuzzleGenerator : IPuzzleGenerator
    {
        public const string Version = "easy-generator-v1";
        public const string ProfileVersion = "easy-profile-v1";
        private const int Size = 5;
        private const ulong LayoutStage = 0x4C41594F5554UL;
        private const ulong ArrowStage = 0x4152524F5753UL;

        public GenerationResult Generate(DifficultyProfile profile, int seed, GenerationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options = options ?? new GenerationOptions();
            var work = new GenerationWork(options, cancellationToken);
            GenerationResult Result(GenerationStatus status, PuzzleDefinition puzzle = null,
                PuzzleAssessmentResult assessment = null, int acceptedAttempt = -1) => new GenerationResult(status,
                    puzzle, assessment, new GenerationMetadata(seed, profile, options, acceptedAttempt, puzzle), work.Snapshot());
            GenerationResult Stopped() => Result(cancellationToken.IsCancellationRequested ? GenerationStatus.Cancelled : GenerationStatus.Exhausted);

            if (profile == null || profile.MaxZeroRows < 0 || profile.MaxZeroColumns < 0)
                return Result(GenerationStatus.InvalidProfile);
            if (!string.Equals(profile.Id, "easy", StringComparison.Ordinal) || profile.Size != Size ||
                profile.MaxCatsPerLine != 2 || profile.MaxZeroRows != 1 || profile.MaxZeroColumns != 1)
                return Result(GenerationStatus.UnsupportedProfile);
            if (cancellationToken.IsCancellationRequested) return Stopped();
            if (options.MaxAttempts == 0) return Stopped();
            if (options.MaxLayoutNodes == 0 || options.MaxLayoutNodesPerAttempt == 0)
            { work.Reject(GenerationRejectionReason.LayoutBudget); return Stopped(); }
            if (options.MaxHumanConstraintEvaluations == 0 || options.MaxHumanConstraintEvaluationsPerCall == 0)
            { work.Reject(GenerationRejectionReason.HumanBudget); return Stopped(); }

            var quotas = QuotaVectors();
            for (var attempt = 0; attempt < options.MaxAttempts; attempt++)
            {
                if (work.IsStopped) return Stopped();
                work.Attempts++;
                var layoutRandom = new SplitMix64Random(SplitMix64Random.AttemptState(seed, attempt, LayoutStage));
                var arrowRandom = new SplitMix64Random(SplitMix64Random.AttemptState(seed, attempt, ArrowStage));
                if (!work.NextInt(layoutRandom, 7, out var totalIndex)) return Stopped();
                var choices = quotas[totalIndex + 4];
                if (!work.NextInt(layoutRandom, choices.Count, out var rowChoice) ||
                    !work.NextInt(layoutRandom, choices.Count, out var columnChoice)) return Stopped();
                if (!TryLayout(choices[rowChoice], choices[columnChoice], layoutRandom, work, out var cats))
                {
                    if (work.IsStopped) return Stopped();
                    work.Reject(GenerationRejectionReason.LayoutBudget);
                    continue;
                }

                var selected = new List<ArrowClue>();
                var barePuzzle = new PuzzleDefinition(Size, seed, Version, cats, selected);
                var pool = TruthfulArrows(barePuzzle);
                var current = work.SolveHuman(barePuzzle);
                if (cancellationToken.IsCancellationRequested) return Stopped();
                if (current.Status == HumanSolverStatus.BudgetExceeded)
                { work.Reject(GenerationRejectionReason.HumanBudget); continue; }
                if (current.Status == HumanSolverStatus.InvalidInput || current.Status == HumanSolverStatus.Contradiction)
                { work.Reject(GenerationRejectionReason.InvalidCandidate); continue; }
                if (current.Status == HumanSolverStatus.Solved)
                {
                    var assessment = work.Assess(barePuzzle, profile);
                    if (cancellationToken.IsCancellationRequested) return Stopped();
                    if (assessment.IsAccepted) return Result(GenerationStatus.Success, barePuzzle, assessment, attempt);
                    work.RejectAssessment(assessment);
                    continue;
                }

                var attemptedCompleteBoard = false;
                for (var stage = 0; stage < options.MaxArrows; stage++)
                {
                    if (work.IsStopped) return Stopped();
                    var available = pool.Where(a => !selected.Any(s => s.Coordinate == a.Coordinate)).ToList();
                    if (!work.Shuffle(available, arrowRandom)) return Stopped();
                    var trialCount = Math.Min(options.ArrowTrialsPerStage, available.Count);
                    HumanSolverResult best = null;
                    var bestArrow = default(ArrowClue);
                    var bestScore = -1;
                    var unknownBefore = new HashSet<BoardCoordinate>(current.Assignments
                        .Where(a => a.State == CellKnowledge.Unknown).Select(a => a.Coordinate));
                    for (var trial = 0; trial < trialCount; trial++)
                    {
                        if (work.IsStopped) return Stopped();
                        var arrow = available[trial];
                        var candidate = new PuzzleDefinition(Size, seed, Version, cats, selected.Concat(new[] { arrow }));
                        work.ArrowTrials++;
                        var solved = work.SolveHuman(candidate);
                        if (cancellationToken.IsCancellationRequested) return Stopped();
                        if (solved.Status == HumanSolverStatus.BudgetExceeded)
                        { work.Reject(GenerationRejectionReason.HumanBudget); continue; }
                        if (solved.Status == HumanSolverStatus.Contradiction || solved.Status == HumanSolverStatus.InvalidInput)
                        { work.Reject(GenerationRejectionReason.InvalidCandidate); continue; }
                        if (solved.Status == HumanSolverStatus.Solved)
                        {
                            attemptedCompleteBoard = true;
                            var assessment = work.Assess(candidate, profile);
                            if (cancellationToken.IsCancellationRequested) return Stopped();
                            if (assessment.IsAccepted) return Result(GenerationStatus.Success, candidate, assessment, attempt);
                            work.RejectAssessment(assessment);
                            break;
                        }
                        var score = solved.Assignments.Count(a => a.Coordinate != arrow.Coordinate &&
                            unknownBefore.Contains(a.Coordinate) && a.State != CellKnowledge.Unknown);
                        if (score <= bestScore) continue;
                        bestScore = score; bestArrow = arrow; best = solved;
                    }
                    if (attemptedCompleteBoard || best == null) break;
                    // Equal scores retain the first candidate in the seeded shuffle.
                    // A zero-gain truthful clue may establish a later deduction chain.
                    selected.Add(bestArrow);
                    current = best;
                }
                if (!attemptedCompleteBoard)
                {
                    work.Reject(GenerationRejectionReason.HumanStalled);
                    work.Reject(GenerationRejectionReason.ArrowLimit);
                }
            }
            return Stopped();
        }

        private static Dictionary<int, List<int[]>> QuotaVectors()
        {
            var vectors = Enumerable.Range(4, 7).ToDictionary(total => total, total => new List<int[]>());
            // Exactly 3^5 canonical quota patterns; no unbounded rejection sampling.
            for (var encoded = 0; encoded < 243; encoded++)
            {
                var value = encoded;
                var quota = new int[Size];
                for (var line = 0; line < Size; line++) { quota[line] = value % 3; value /= 3; }
                if (quota.Count(q => q == 0) <= 1) vectors[quota.Sum()].Add(quota);
            }
            return vectors;
        }

        private static bool TryLayout(int[] rows, int[] columns, SplitMix64Random random, GenerationWork work,
            out IReadOnlyList<BoardCoordinate> cats)
        {
            cats = null;
            var rowOrder = Enumerable.Range(0, Size).ToList();
            if (!work.Shuffle(rowOrder, random)) return false;
            var remaining = (int[])columns.Clone();
            var chosen = new int[Size];
            var attemptNodes = 0;
            var localBudgetHit = false;

            bool Visit(int depth)
            {
                if (work.IsStopped || localBudgetHit) return false;
                if (attemptNodes >= work.Options.MaxLayoutNodesPerAttempt)
                { localBudgetHit = true; return false; }
                if (!work.VisitLayoutNode()) return false;
                attemptNodes++;
                if (depth == Size) return remaining.All(count => count == 0);
                var row = rowOrder[depth];
                var patterns = new List<int>();
                for (var mask = 0; mask < 32; mask++)
                {
                    var count = 0;
                    var possible = true;
                    for (var column = 0; column < Size; column++)
                    {
                        if ((mask & (1 << column)) == 0) continue;
                        count++;
                        if (remaining[column] == 0) possible = false;
                    }
                    if (possible && count == rows[row]) patterns.Add(mask);
                }
                if (!work.Shuffle(patterns, random)) return false;
                foreach (var mask in patterns)
                {
                    if (work.IsStopped || localBudgetHit) return false;
                    chosen[row] = mask;
                    for (var column = 0; column < Size; column++)
                        if ((mask & (1 << column)) != 0) remaining[column]--;
                    var rowsLeft = Size - depth - 1;
                    var availableColumns = remaining.Count(count => count > 0);
                    var feasible = remaining.All(count => count <= rowsLeft) &&
                        rowOrder.Skip(depth + 1).All(nextRow => rows[nextRow] <= availableColumns);
                    if (feasible && Visit(depth + 1)) return true;
                    for (var column = 0; column < Size; column++)
                        if ((mask & (1 << column)) != 0) remaining[column]++;
                }
                chosen[row] = 0;
                return false;
            }

            if (!Visit(0)) return false;
            cats = Array.AsReadOnly(Enumerable.Range(0, Size).SelectMany(row => Enumerable.Range(0, Size)
                .Where(column => (chosen[row] & (1 << column)) != 0).Select(column => new BoardCoordinate(row, column))).ToArray());
            return true;
        }

        private static List<ArrowClue> TruthfulArrows(PuzzleDefinition puzzle)
        {
            var arrows = new List<ArrowClue>();
            for (var row = 0; row < Size; row++)
            for (var column = 0; column < Size; column++)
            {
                var coordinate = new BoardCoordinate(row, column);
                if (puzzle.IsSolutionCat(coordinate)) continue;
                for (var value = 0; value < 4; value++)
                {
                    var direction = (Direction)value;
                    if (puzzle.GetDirectionalSegment(coordinate, direction).Any(puzzle.IsSolutionCat))
                        arrows.Add(new ArrowClue(coordinate, direction));
                }
            }
            return arrows;
        }

        private sealed class GenerationWork
        {
            public GenerationOptions Options { get; }
            private readonly CancellationToken _cancellationToken;
            private readonly Dictionary<GenerationRejectionReason, int> _rejections =
                Enum.GetValues(typeof(GenerationRejectionReason)).Cast<GenerationRejectionReason>().ToDictionary(reason => reason, reason => 0);
            private bool _randomStopped;
            private bool _layoutStopped;
            private bool _verificationStopped;
            public int Attempts { get; set; }
            public int LayoutNodes { get; private set; }
            public int HumanEvaluations { get; private set; }
            public int VerifierNodes { get; private set; }
            public int RandomDraws { get; private set; }
            public int ArrowTrials { get; set; }
            public bool IsStopped => _cancellationToken.IsCancellationRequested || _randomStopped || _layoutStopped || _verificationStopped ||
                HumanEvaluations >= Options.MaxHumanConstraintEvaluations;

            public GenerationWork(GenerationOptions options, CancellationToken cancellationToken)
            { Options = options; _cancellationToken = cancellationToken; }
            public void Reject(GenerationRejectionReason reason) { _rejections[reason]++; }
            public GenerationDiagnostics Snapshot() => new GenerationDiagnostics(Attempts, LayoutNodes, HumanEvaluations,
                VerifierNodes, RandomDraws, ArrowTrials, _rejections);

            public bool NextInt(SplitMix64Random random, int exclusiveMax, out int value)
            {
                value = 0;
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (RandomDraws >= Options.MaxRandomDraws)
                    { _randomStopped = true; Reject(GenerationRejectionReason.RandomBudget); return false; }
                    RandomDraws++;
                    if (random.TryNextInt(exclusiveMax, 1, out value)) return true;
                }
                return false;
            }

            public bool Shuffle<T>(IList<T> values, SplitMix64Random random)
            {
                for (var i = values.Count - 1; i > 0; i--)
                {
                    if (!NextInt(random, i + 1, out var selected)) return false;
                    var value = values[i]; values[i] = values[selected]; values[selected] = value;
                }
                return !_cancellationToken.IsCancellationRequested;
            }

            public bool VisitLayoutNode()
            {
                if (LayoutNodes >= Options.MaxLayoutNodes)
                { _layoutStopped = true; Reject(GenerationRejectionReason.LayoutBudget); return false; }
                LayoutNodes++;
                return true;
            }

            public HumanSolverResult SolveHuman(PuzzleDefinition puzzle)
            {
                var allowance = Math.Min(Options.MaxHumanConstraintEvaluationsPerCall, Options.MaxHumanConstraintEvaluations - HumanEvaluations);
                var result = new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(puzzle), new SolverOptions(allowance, 0));
                HumanEvaluations += result.Metrics.ConstraintEvaluations;
                return result;
            }

            public PuzzleAssessmentResult Assess(PuzzleDefinition puzzle, DifficultyProfile profile)
            {
                var humanAllowance = Math.Min(Options.MaxHumanConstraintEvaluationsPerCall, Options.MaxHumanConstraintEvaluations - HumanEvaluations);
                var verifierAllowance = Math.Min(Options.MaxVerifierNodesPerCall, Options.MaxVerifierNodes - VerifierNodes);
                var assessment = PuzzleSolvabilityAssessment.Assess(puzzle, profile, new SolverOptions(humanAllowance, verifierAllowance));
                HumanEvaluations += assessment.Solver.Human.Metrics.ConstraintEvaluations;
                VerifierNodes += assessment.Solver.Uniqueness.NodesVisited;
                return assessment;
            }

            public void RejectAssessment(PuzzleAssessmentResult assessment)
            {
                if (assessment.Solver.Human.Status == HumanSolverStatus.BudgetExceeded) Reject(GenerationRejectionReason.HumanBudget);
                if (assessment.Solver.Uniqueness.Status == UniquenessStatus.Incomplete) Reject(GenerationRejectionReason.VerificationIncomplete);
                if (assessment.Solver.Uniqueness.Status == UniquenessStatus.Incomplete &&
                    (VerifierNodes >= Options.MaxVerifierNodes || Options.MaxVerifierNodesPerCall == 0))
                    _verificationStopped = true;
                if (assessment.Solver.Uniqueness.Status == UniquenessStatus.Multiple) Reject(GenerationRejectionReason.Ambiguous);
                if (assessment.Status == PuzzleAssessmentStatus.InvalidCandidate || assessment.Status == PuzzleAssessmentStatus.ProfileRejected)
                    Reject(GenerationRejectionReason.InvalidCandidate);
                if (assessment.Status == PuzzleAssessmentStatus.IntendedSolutionMismatch) Reject(GenerationRejectionReason.AnswerMismatch);
            }
        }
    }
}
