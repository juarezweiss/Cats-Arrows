using System;
using System.Collections.Generic;

namespace CatsAndArrows.Core.Localization
{
    /// <summary>Presentation of structured proof steps. Puzzle solvers never receive this service.</summary>
    public sealed class DeductionLocalizer
    {
        private readonly LocalizationService text;
        public DeductionLocalizer(LocalizationService text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public string Format(HumanSolverStatus status)
        {
            switch (status)
            {
                case HumanSolverStatus.Solved: return text.GetText("solver.solved");
                case HumanSolverStatus.Stalled: return text.GetText("solver.stalled");
                case HumanSolverStatus.Contradiction: return text.GetText("solver.contradiction");
                case HumanSolverStatus.InvalidInput: return text.GetText("solver.invalid-input");
                case HumanSolverStatus.BudgetExceeded: return text.GetText("solver.budget-exceeded");
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        public string Format(UniquenessStatus status)
        {
            switch (status)
            {
                case UniquenessStatus.Unique: return text.GetText("uniqueness.unique");
                case UniquenessStatus.Multiple: return text.GetText("uniqueness.multiple");
                case UniquenessStatus.NoSolution: return text.GetText("uniqueness.no-solution");
                case UniquenessStatus.InvalidInput: return text.GetText("uniqueness.invalid-input");
                case UniquenessStatus.Incomplete: return text.GetText("uniqueness.incomplete");
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        public string Format(DeductionStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            var values = new Dictionary<string, object>();
            foreach (var source in step.Sources)
            {
                if (source.Kind == ConstraintKind.Arrow)
                {
                    values["row"] = source.Coordinate.Row + 1;
                    values["column"] = source.Coordinate.Column + 1;
                    values["direction"] = text.GetText("direction." + source.Direction.ToString().ToLowerInvariant());
                }
                else
                {
                    values["line"] = text.GetText(source.Kind == ConstraintKind.Row ? "line.row" : "line.column");
                    values["index"] = source.Index + 1;
                }
            }
            foreach (var premise in step.Premises)
            {
                if (premise.Source.Kind == ConstraintKind.Arrow) continue;
                values["total"] = premise.RequiredPaws;
                values["count"] = premise.RequiredPaws - premise.KnownPawCount;
            }
            return text.Format("deduction." + step.RuleId, values);
        }
    }
}
