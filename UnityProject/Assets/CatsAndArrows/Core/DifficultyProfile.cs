using System;

namespace CatsAndArrows.Core
{
    public sealed class DifficultyProfile
    {
        public string Id { get; }
        public int Size { get; }
        public int MaxCatsPerLine { get; }
        public int MaxZeroRows { get; }
        public int MaxZeroColumns { get; }
        public int? RequiredCatCountInSomeLine { get; }

        public DifficultyProfile(string id, int size, int maxCatsPerLine, int maxZeroRows = 1, int maxZeroColumns = 1, int? requiredCatCountInSomeLine = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Profile id is required.", nameof(id));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (maxCatsPerLine <= 0) throw new ArgumentOutOfRangeException(nameof(maxCatsPerLine));

            Id = id;
            Size = size;
            MaxCatsPerLine = maxCatsPerLine;
            MaxZeroRows = maxZeroRows;
            MaxZeroColumns = maxZeroColumns;
            if (requiredCatCountInSomeLine < 0 || requiredCatCountInSomeLine > maxCatsPerLine)
                throw new ArgumentOutOfRangeException(nameof(requiredCatCountInSomeLine));
            RequiredCatCountInSomeLine = requiredCatCountInSomeLine;
        }

        public static DifficultyProfile Easy => new DifficultyProfile("easy", 5, 2);
        public static DifficultyProfile Medium => new DifficultyProfile("medium", 6, 3);
        public static DifficultyProfile Hard => new DifficultyProfile("hard", 7, 3, 0, 0, 3);
    }
}
