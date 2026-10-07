using System;

namespace CatsAndArrows.Core
{
    // SplitMix64: fixed unsigned arithmetic and full 64-bit output. Vectors lock
    // this implementation independently of runtime, Unity, and current culture.
    public sealed class SplitMix64Random
    {
        public const string Version = "splitmix64-v1";
        private ulong _state;
        public long DrawsUsed { get; private set; }
        public SplitMix64Random(ulong initialState) { _state = initialState; }
        public static ulong SeedToState(int seed) => unchecked((ulong)(uint)seed);

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                DrawsUsed++;
                return Mix(_state);
            }
        }

        // Rejection draws are bounded, including when no value is produced.
        public bool TryNextInt(int exclusiveMax, int maxDraws, out int value)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            if (maxDraws < 0) throw new ArgumentOutOfRangeException(nameof(maxDraws));
            value = 0;
            var bound = (ulong)exclusiveMax;
            var threshold = unchecked(0UL - bound) % bound;
            for (var draw = 0; draw < maxDraws; draw++)
            {
                var sample = NextUInt64();
                if (sample < threshold) continue;
                value = (int)(sample % bound);
                return true;
            }
            return false;
        }

        internal static ulong AttemptState(int seed, int attempt, ulong stage)
        {
            // State derivation is fixed arithmetic, not a consumable random draw.
            return Mix(unchecked(SeedToState(seed) + (ulong)attempt * 0xD1B54A32D192ED03UL + stage + 0x9E3779B97F4A7C15UL));
        }

        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
