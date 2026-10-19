using System;

namespace CatsAndArrows.Core
{
    public sealed class DifficultyProfile
    {
        public string Id { get; }
        public int Size { get; }
        public int MaxPawsPerLine { get; }
        public int MaxZeroRows { get; }
        public int MaxZeroColumns { get; }
        public int? RequiredPawCountInSomeLine { get; }

        public DifficultyProfile(string id, int size, int maxPawsPerLine, int maxZeroRows = 1, int maxZeroColumns = 1, int? requiredPawCountInSomeLine = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Profile id is required.", nameof(id));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (maxPawsPerLine <= 0) throw new ArgumentOutOfRangeException(nameof(maxPawsPerLine));

            Id = id;
            Size = size;
            MaxPawsPerLine = maxPawsPerLine;
            MaxZeroRows = maxZeroRows;
            MaxZeroColumns = maxZeroColumns;
            if (requiredPawCountInSomeLine < 0 || requiredPawCountInSomeLine > maxPawsPerLine)
                throw new ArgumentOutOfRangeException(nameof(requiredPawCountInSomeLine));
            RequiredPawCountInSomeLine = requiredPawCountInSomeLine;
        }

        public static DifficultyProfile Easy => new DifficultyProfile("easy", 5, 2);
        public static DifficultyProfile Medium => new DifficultyProfile("medium", 6, 3);
        public static DifficultyProfile Hard => new DifficultyProfile("hard", 7, 3, 0, 0, 3);
    }
}
