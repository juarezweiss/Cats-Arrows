using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public sealed class HumanDeductionSolver
    {
        public HumanSolverResult Solve(PuzzleClues clues, SolverOptions options = null)
        {
            options = options ?? new SolverOptions();
            var errors = clues == null ? new[] { "Clues are required." } : clues.ValidateInput();
            if (errors.Count > 0)
                return new HumanSolverResult(HumanSolverStatus.InvalidInput, Array.Empty<CellAssignment>(), Array.Empty<DeductionStep>(),
                    new SolverMetrics(Array.Empty<DeductionStep>(), 0, 0, 0, 0), errors);
            var constraints = new ClueConstraints(clues);
            var states = constraints.InitialStates();
            var steps = new List<DeductionStep>();
            var originStep = Enumerable.Repeat(-1, states.Length).ToArray();
            var evaluations = 0;
            var maxCandidates = constraints.Rows.Concat(constraints.Columns).Max(c => c.Cells.Length);

            HumanSolverResult Result(HumanSolverStatus status, string error = null) => new HumanSolverResult(status,
                constraints.Assignments(states), steps, new SolverMetrics(steps, clues.Size, clues.Arrows.Count, maxCandidates, evaluations),
                error == null ? Array.Empty<string>() : new[] { error });

            var initialContradiction = constraints.InitialContradiction();
            if (initialContradiction != null) return Result(HumanSolverStatus.Contradiction, initialContradiction);

            while (true)
            {
                var progressed = false;
                foreach (var constraint in constraints.All)
                {
                    if (evaluations >= options.MaxHumanConstraintEvaluations) return Result(HumanSolverStatus.BudgetExceeded);
                    evaluations++;
                    var paws = constraint.Cells.Count(i => states[i] == CellKnowledge.Paw);
                    var unknown = constraint.Cells.Where(i => states[i] == CellKnowledge.Unknown).ToArray();
                    var remaining = constraint.RequiredPaws - paws;
                    if ((constraint.IsExact && (remaining < 0 || remaining > unknown.Length)) ||
                        (!constraint.IsExact && paws == 0 && unknown.Length == 0))
                        return Result(HumanSolverStatus.Contradiction, "A visible clue has no feasible completion.");

                    string ruleId = null;
                    var kind = DeductionKind.ConstraintPropagation;
                    var conclusionState = CellKnowledge.NotPaw;
                    var affected = Array.Empty<int>();
                    var sources = new[] { constraint };
                    if (constraint.IsExact && unknown.Length > 0)
                    {
                        if (remaining == 0)
                        {
                            ruleId = constraint.RequiredPaws == 0 ? DeductionRules.ZeroLine : DeductionRules.LineSatisfied;
                            kind = constraint.RequiredPaws == 0 ? DeductionKind.ZeroLine : DeductionKind.LineSatisfied;
                            affected = unknown;
                        }
                        else if (remaining == unknown.Length)
                        {
                            ruleId = DeductionRules.FillRemainingLine; kind = DeductionKind.FillRemainingLine;
                            affected = unknown; conclusionState = CellKnowledge.Paw;
                        }
                    }
                    else if (!constraint.IsExact && paws == 0)
                    {
                        if (unknown.Length == 1)
                        {
                            ruleId = DeductionRules.ArrowSingleton; kind = DeductionKind.ArrowSegment;
                            affected = unknown; conclusionState = CellKnowledge.Paw;
                        }
                        else if (DirectionGeometry.IsCardinal(constraint.Source.Direction))
                        {
                            // Only cardinal rays lie wholly in one exact row or
                            // column. Diagonal rays have no corresponding total.
                            var arrow = constraint.Source;
                            var line = arrow.Direction == Direction.Left || arrow.Direction == Direction.Right
                                ? constraints.Rows[arrow.Coordinate.Row] : constraints.Columns[arrow.Coordinate.Column];
                            var linePaws = line.Cells.Count(i => states[i] == CellKnowledge.Paw);
                            if (line.RequiredPaws - linePaws == 1)
                            {
                                affected = line.Cells.Where(i => states[i] == CellKnowledge.Unknown && !constraint.Cells.Contains(i)).ToArray();
                                if (affected.Length > 0)
                                {
                                    ruleId = DeductionRules.ArrowLineExclusion; kind = DeductionKind.ArrowLineExclusion;
                                    sources = new[] { constraint, line };
                                }
                            }
                        }
                    }
                    if (ruleId == null) continue;
                    var premises = sources.Select(source => new DeductionPremise(source.Source, source.RequiredPaws,
                        source.Cells.Select(i => new CellAssignment(constraints.Coordinate(i), states[i])))).ToArray();
                    var dependencies = sources.SelectMany(s => s.Cells).Select(i => originStep[i]).Where(i => i >= 0);
                    var conclusions = affected.Select(i => new CellAssignment(constraints.Coordinate(i), conclusionState)).ToArray();
                    var step = new DeductionStep(steps.Count, ruleId, kind, premises, conclusions, dependencies);
                    foreach (var i in affected) { states[i] = conclusionState; originStep[i] = step.Index; }
                    steps.Add(step);
                    progressed = true;
                    // Propagate after every atomic deduction batch, in canonical constraint order.
                    break;
                }
                if (!progressed)
                    return Result(states.All(s => s != CellKnowledge.Unknown) ? HumanSolverStatus.Solved : HumanSolverStatus.Stalled);
            }
        }
    }
}
