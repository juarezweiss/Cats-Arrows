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
        public string EffectiveVersion { get; }
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
            : this(maxAttempts, maxLayoutNodesPerAttempt, maxLayoutNodes, arrowTrialsPerStage, maxArrows,
                maxHumanConstraintEvaluationsPerCall, maxHumanConstraintEvaluations, maxVerifierNodesPerCall,
                maxVerifierNodes, maxRandomDraws, false) { }

        // Issue #32: separate versioned work limits; Easy defaults and public v1 ceilings remain unchanged.
        public static GenerationOptions ForLargeBoard() => new GenerationOptions(64, 512, 32768, 8, 24,
            2000, 200000, 5000, 50000, 100000, true);

        private GenerationOptions(int maxAttempts, int maxLayoutNodesPerAttempt, int maxLayoutNodes,
            int arrowTrialsPerStage, int maxArrows, int maxHumanConstraintEvaluationsPerCall,
            int maxHumanConstraintEvaluations, int maxVerifierNodesPerCall, int maxVerifierNodes,
            int maxRandomDraws, bool largeBoard)
        {
            EffectiveVersion = largeBoard ? "generation-options-large-v1" : Version;
            MaxAttempts = NonNegative(maxAttempts, nameof(maxAttempts));
            MaxLayoutNodesPerAttempt = NonNegative(maxLayoutNodesPerAttempt, nameof(maxLayoutNodesPerAttempt));
            MaxLayoutNodes = NonNegative(maxLayoutNodes, nameof(maxLayoutNodes));
            ArrowTrialsPerStage = NonNegative(arrowTrialsPerStage, nameof(arrowTrialsPerStage));
            MaxArrows = NonNegative(maxArrows, nameof(maxArrows));
            if (arrowTrialsPerStage > 8) throw new ArgumentOutOfRangeException(nameof(arrowTrialsPerStage), "V1 permits up to eight trials per stage.");
            if (maxArrows > (largeBoard ? 24 : 10)) throw new ArgumentOutOfRangeException(nameof(maxArrows), "V1 permits up to ten arrows.");
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
        public string GeneratorVersion { get; }
        public DifficultyProfile Profile { get; }
        public string ProfileId { get; }
        public string ProfileVersion { get; }
        public string RulesetVersion => DeductionRules.RulesetVersion;
        public string VerifierVersion => UniquenessResult.VerifierVersion;
        public string RandomVersion => SplitMix64Random.Version;
        public string OptionsVersion => Options.EffectiveVersion;
        public GenerationOptions Options { get; }
        public int AcceptedAttempt { get; }
        public string BoardFingerprint { get; }
        public string SolutionFingerprint { get; }
        internal GenerationMetadata(int seed, DifficultyProfile profile, GenerationOptions options, int acceptedAttempt, PuzzleDefinition puzzle,
            string generatorVersion = SeededEasyPuzzleGenerator.Version, string profileVersion = SeededEasyPuzzleGenerator.ProfileVersion)
        {
            GeneratorVersion = generatorVersion; ProfileVersion = profileVersion;
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
