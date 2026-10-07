using System.Threading;

namespace CatsAndArrows.Core
{
    public interface IPuzzleGenerator
    {
        GenerationResult Generate(DifficultyProfile profile, int seed, GenerationOptions options = null,
            CancellationToken cancellationToken = default);
    }
}
