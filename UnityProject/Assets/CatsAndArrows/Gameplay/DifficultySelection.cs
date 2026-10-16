using System;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public sealed class DifficultyHistory
    {
        public const int CurrentVersion = 1;
        public int Version { get; }
        public string LastDifficulty { get; }
        public int ConsecutiveStarts { get; }
        public DifficultyHistory(string lastDifficulty, int consecutiveStarts, int version = CurrentVersion)
        { LastDifficulty = lastDifficulty; ConsecutiveStarts = consecutiveStarts; Version = version; }
    }

    public interface IDifficultyHistoryStore
    {
        DifficultyHistory Load();
        bool TrySave(DifficultyHistory history);
    }

    public sealed class MemoryDifficultyHistoryStore : IDifficultyHistoryStore
    {
        private DifficultyHistory history;
        public DifficultyHistory Load() => history;
        public bool TrySave(DifficultyHistory value) { history = value; return true; }
    }

    // Issue #43: selection owns no puzzle generation or Unity state. Failed starts
    // keep their pending choice; only an admitted ordinary session commits a run.
    public sealed class DifficultySelection
    {
        private static readonly DifficultyProfile[] Modes =
            { DifficultyProfile.Easy, DifficultyProfile.Medium, DifficultyProfile.Hard };
        private readonly Func<int, int> nextIndex;
        private readonly IDifficultyHistoryStore store;
        public DifficultyHistory History { get; private set; }
        public DifficultyProfile Pending { get; private set; }

        public DifficultySelection(Func<int, int> nextIndex, IDifficultyHistoryStore store = null)
        {
            this.nextIndex = nextIndex ?? throw new ArgumentNullException(nameof(nextIndex));
            this.store = store ?? new MemoryDifficultyHistoryStore();
            var saved = this.store.Load();
            if (saved != null && saved.Version == DifficultyHistory.CurrentVersion &&
                saved.ConsecutiveStarts >= 1 && saved.ConsecutiveStarts <= 2 &&
                Array.Exists(Modes, mode => mode.Id == saved.LastDifficulty)) History = saved;
        }

        public static DifficultySelection CreateSeeded(int seed, IDifficultyHistoryStore store = null)
        {
            var random = new SplitMix64Random(SplitMix64Random.SeedToState(seed));
            return new DifficultySelection(bound =>
            {
                if (!random.TryNextInt(bound, 128, out var index))
                    throw new InvalidOperationException("Difficulty random draw budget exhausted.");
                return index;
            }, store);
        }

        public DifficultyProfile Choose()
        {
            if (Pending != null) return Pending;
            var excluded = History?.ConsecutiveStarts == 2 ? History.LastDifficulty : null;
            var eligible = Array.FindAll(Modes, mode => mode.Id != excluded);
            var index = nextIndex(eligible.Length);
            if (index < 0 || index >= eligible.Length)
                throw new InvalidOperationException("Random choice must be an eligible index.");
            Pending = eligible[index];
            return Pending;
        }

        public bool CommitStart()
        {
            if (Pending == null) return false;
            var count = History?.LastDifficulty == Pending.Id ? History.ConsecutiveStarts + 1 : 1;
            var committed = new DifficultyHistory(Pending.Id, count);
            if (!store.TrySave(committed)) return false;
            History = committed;
            Pending = null;
            return true;
        }
    }
}
