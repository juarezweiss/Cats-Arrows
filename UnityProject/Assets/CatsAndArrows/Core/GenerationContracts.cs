using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CatsAndArrows.Core
{
    public enum GenerationStatus { Success, InvalidProfile, UnsupportedProfile, Exhausted, Cancelled }
    public enum GenerationRejectionReason
    {
        LayoutBudget, ArrowLimit, HumanStalled, HumanBudget, VerificationIncomplete,
        Ambiguous, InvalidCandidate, AnswerMismatch, RandomBudget
    }

    public sealed class GenerationOptions
    {
        public const string Version = "generation-options-v1";
        public int MaxAttempts { get; }
        public int MaxLayoutNodesPerAttempt { get; }
        public int MaxLayoutNodes { get; }
        public int ArrowTrialsPerStage { get; }
        public int MaxArrows { get; }
        public int MaxHumanConstraintEvaluationsPerCall { get; }
        public int MaxHumanConstraintEvaluations { get; }
        public int MaxVerifierNodesPerCall { get; }
        public int MaxVerifierNodes { get; }
        public int MaxRandomDraws { get; }

        public GenerationOptions(int maxAttempts = 64, int maxLayoutNodesPerAttempt = 256, int maxLayoutNodes = 16384,
            int arrowTrialsPerStage = 8, int maxArrows = 10, int maxHumanConstraintEvaluationsPerCall = 500,
            int maxHumanConstraintEvaluations = 200000, int maxVerifierNodesPerCall = 5000,
            int maxVerifierNodes = 50000, int maxRandomDraws = 100000)
        {
            MaxAttempts = NonNegative(maxAttempts, nameof(maxAttempts));
            MaxLayoutNodesPerAttempt = NonNegative(maxLayoutNodesPerAttempt, nameof(maxLayoutNodesPerAttempt));
            MaxLayoutNodes = NonNegative(maxLayoutNodes, nameof(maxLayoutNodes));
            ArrowTrialsPerStage = NonNegative(arrowTrialsPerStage, nameof(arrowTrialsPerStage));
            MaxArrows = NonNegative(maxArrows, nameof(maxArrows));
            if (arrowTrialsPerStage > 8) throw new ArgumentOutOfRangeException(nameof(arrowTrialsPerStage), "V1 permits up to eight trials per stage.");
            if (maxArrows > 10) throw new ArgumentOutOfRangeException(nameof(maxArrows), "V1 permits up to ten arrows.");
            MaxHumanConstraintEvaluationsPerCall = NonNegative(maxHumanConstraintEvaluationsPerCall, nameof(maxHumanConstraintEvaluationsPerCall));
            MaxHumanConstraintEvaluations = NonNegative(maxHumanConstraintEvaluations, nameof(maxHumanConstraintEvaluations));
            MaxVerifierNodesPerCall = NonNegative(maxVerifierNodesPerCall, nameof(maxVerifierNodesPerCall));
            MaxVerifierNodes = NonNegative(maxVerifierNodes, nameof(maxVerifierNodes));
            MaxRandomDraws = NonNegative(maxRandomDraws, nameof(maxRandomDraws));
        }

        private static int NonNegative(int value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public sealed class GenerationMetadata
    {
        public int Seed { get; }
        public string GeneratorVersion => SeededEasyPuzzleGenerator.Version;
        public DifficultyProfile Profile { get; }
        public string ProfileId { get; }
        public string ProfileVersion => SeededEasyPuzzleGenerator.ProfileVersion;
        public string RulesetVersion => DeductionRules.RulesetVersion;
        public string VerifierVersion => UniquenessResult.VerifierVersion;
        public string RandomVersion => SplitMix64Random.Version;
        public string OptionsVersion => GenerationOptions.Version;
        public GenerationOptions Options { get; }
        public int AcceptedAttempt { get; }
        public string BoardFingerprint { get; }
        public string SolutionFingerprint { get; }
        internal GenerationMetadata(int seed, DifficultyProfile profile, GenerationOptions options, int acceptedAttempt, PuzzleDefinition puzzle)
        {
            Seed = seed; Profile = profile; ProfileId = profile?.Id; Options = options; AcceptedAttempt = acceptedAttempt;
            if (puzzle == null) return;
            BoardFingerprint = GenerationFingerprints.ForBoard(puzzle);
            SolutionFingerprint = GenerationFingerprints.ForSolution(puzzle);
        }
    }

    public sealed class GenerationDiagnostics
    {
        public int Attempts { get; }
        public int LayoutNodesVisited { get; }
        public int HumanConstraintEvaluations { get; }
        public int VerifierNodesVisited { get; }
        public int RandomDraws { get; }
        public int ArrowTrials { get; }
        public IReadOnlyDictionary<GenerationRejectionReason, int> RejectionCounts { get; }
        internal GenerationDiagnostics(int attempts, int layoutNodes, int humanEvaluations, int verifierNodes,
            int randomDraws, int arrowTrials, IDictionary<GenerationRejectionReason, int> rejections)
        {
            Attempts = attempts; LayoutNodesVisited = layoutNodes; HumanConstraintEvaluations = humanEvaluations;
            VerifierNodesVisited = verifierNodes; RandomDraws = randomDraws; ArrowTrials = arrowTrials;
            RejectionCounts = new ReadOnlyDictionary<GenerationRejectionReason, int>(new Dictionary<GenerationRejectionReason, int>(rejections));
        }
    }

    public sealed class GenerationResult
    {
        public GenerationStatus Status { get; }
        public bool IsSuccess => Status == GenerationStatus.Success;
        public PuzzleDefinition Puzzle { get; }
        public PuzzleAssessmentResult Assessment { get; }
        public GenerationMetadata Metadata { get; }
        public GenerationDiagnostics Diagnostics { get; }
        internal GenerationResult(GenerationStatus status, PuzzleDefinition puzzle, PuzzleAssessmentResult assessment,
            GenerationMetadata metadata, GenerationDiagnostics diagnostics)
        { Status = status; Puzzle = puzzle; Assessment = assessment; Metadata = metadata; Diagnostics = diagnostics; }
    }
}
