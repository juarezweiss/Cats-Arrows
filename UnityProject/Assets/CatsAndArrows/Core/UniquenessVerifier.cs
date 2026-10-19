using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public sealed class UniquenessVerifier
    {
        public UniquenessResult Verify(PuzzleClues clues, SolverOptions options = null)
        {
            options = options ?? new SolverOptions();
            var errors = clues == null ? new[] { "Clues are required." } : clues.ValidateInput();
            if (errors.Count > 0)
                return new UniquenessResult(UniquenessStatus.InvalidInput, Array.Empty<IReadOnlyList<BoardCoordinate>>(), 0, errors);
            var constraints = new ClueConstraints(clues);
            var solutions = new List<IReadOnlyList<BoardCoordinate>>();
            var nodes = 0;
            var incomplete = false;
            var initialContradiction = constraints.InitialContradiction();
            if (initialContradiction != null)
                return new UniquenessResult(UniquenessStatus.NoSolution, solutions, 0, new[] { initialContradiction });

            void Search(CellKnowledge[] states)
            {
                if (solutions.Count >= 2 || incomplete) return;
                if (nodes >= options.MaxVerifierNodes) { incomplete = true; return; }
                nodes++;
                if (!PropagateBounds(constraints, states)) return;
                var next = Array.IndexOf(states, CellKnowledge.Unknown);
                if (next < 0)
                {
                    // Recheck every full assignment independently of propagation and the human solver.
                    if (constraints.All.All(c => c.IsExact
                        ? c.Cells.Count(i => states[i] == CellKnowledge.Paw) == c.RequiredPaws
                        : c.Cells.Any(i => states[i] == CellKnowledge.Paw)))
                        solutions.Add(Array.AsReadOnly(states.Select((state, i) => new { state, i })
                            .Where(p => p.state == CellKnowledge.Paw).Select(p => constraints.Coordinate(p.i)).ToArray()));
                    return;
                }
                // Deterministic row-major variable order, Paw branch before NotPaw.
                var pawBranch = (CellKnowledge[])states.Clone();
                pawBranch[next] = CellKnowledge.Paw;
                Search(pawBranch);
                if (solutions.Count >= 2 || incomplete) return;
                var notPawBranch = (CellKnowledge[])states.Clone();
                notPawBranch[next] = CellKnowledge.NotPaw;
                Search(notPawBranch);
            }

            Search(constraints.InitialStates());
            var status = solutions.Count >= 2 ? UniquenessStatus.Multiple : incomplete ? UniquenessStatus.Incomplete :
                solutions.Count == 1 ? UniquenessStatus.Unique : UniquenessStatus.NoSolution;
            return new UniquenessResult(status, solutions, nodes, Array.Empty<string>());
        }

        // Independent search pruning: exact cardinality bounds and existence bounds.
        // No human traces, intended answers, or gameplay marks enter this method.
        private static bool PropagateBounds(ClueConstraints constraints, CellKnowledge[] states)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var constraint in constraints.All)
                {
                    var paws = constraint.Cells.Count(i => states[i] == CellKnowledge.Paw);
                    var unknown = constraint.Cells.Where(i => states[i] == CellKnowledge.Unknown).ToArray();
                    CellKnowledge? forced = null;
                    if (constraint.IsExact)
                    {
                        if (paws > constraint.RequiredPaws || paws + unknown.Length < constraint.RequiredPaws) return false;
                        if (paws == constraint.RequiredPaws) forced = CellKnowledge.NotPaw;
                        else if (paws + unknown.Length == constraint.RequiredPaws) forced = CellKnowledge.Paw;
                    }
                    else if (paws == 0)
                    {
                        if (unknown.Length == 0) return false;
                        if (unknown.Length == 1) forced = CellKnowledge.Paw;
                    }
                    if (!forced.HasValue) continue;
                    foreach (var i in unknown) { states[i] = forced.Value; changed = true; }
                }
            } while (changed);
            return true;
        }
    }
}
