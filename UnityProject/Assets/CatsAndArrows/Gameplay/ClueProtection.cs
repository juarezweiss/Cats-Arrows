using System;
using System.Collections.Generic;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public static class ClueProtection
    {
        public static IReadOnlyList<BoardCoordinate> Derive(PuzzleClues clues)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            var proof = new HumanDeductionSolver().Solve(clues);
            return ClueProtectionRules.Derive(clues, proof);
        }
    }
}
