namespace CatsAndArrows.Core
{
    public interface IPuzzleGenerator
    {
        PuzzleDefinition Generate(DifficultyProfile profile, int seed);
    }
}
