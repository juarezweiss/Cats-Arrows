using System;
using System.Collections.Generic;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public static class ClueProtection
    {
        public static IReadOnlyList<BoardCoordinate> Derive(PuzzleClues clues, DifficultyProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            // Hard leaves every normal cell for the player to classify. Do not
            // run the presentation solver or derive opposite-side consequences.
            return profile.Id == "hard" ? ClueProtectionRules.ForProfile(profile, clues, null) : Derive(clues);
        }

        public static IReadOnlyList<BoardCoordinate> Derive(PuzzleClues clues)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            var proof = new HumanDeductionSolver().Solve(clues);
            return ClueProtectionRules.Derive(clues, proof);
        }
    }
}
