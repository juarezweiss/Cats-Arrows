using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using UnityEngine;

namespace CatsAndArrows.UI
{
    // Issue #47: presentation-only recognition. This controller never sends mark intents.
    public sealed class ArrowHoldController
    {
        public const double HoldSeconds = .35;
        private readonly GameSession session;
        private readonly BoardLayout layout;
        private readonly Func<double> clock;
        private readonly Func<BoardCoordinate, bool> canBegin;
        private readonly Action<BoardCoordinate> activated;
        private readonly float tolerance;
        private Vector2 pressPosition;
        private double startedAt;
        public bool HasActivePointer { get; private set; }
        public int ActivePointerId { get; private set; } = -1;
        public bool IsHighlighted { get; private set; }
        public BoardCoordinate Origin { get; private set; }
        public IReadOnlyList<BoardCoordinate> Path { get; private set; } = Array.Empty<BoardCoordinate>();

        public ArrowHoldController(GameSession session, BoardLayout layout, Func<double> clock, float tolerance = 8,
            Func<BoardCoordinate, bool> canBegin = null, Action<BoardCoordinate> activated = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (layout.Size != session.Puzzle.Size || layout.CellSize <= 0)
                throw new ArgumentException("Layout must match the session.", nameof(layout));
            if (!float.IsFinite(tolerance) || tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
            this.layout = layout;
            this.tolerance = tolerance;
            this.canBegin = canBegin;
            this.activated = activated;
        }

        public bool Begin(int pointerId, Vector2 position)
        {
            if (HasActivePointer || session.IsCompleted || !Finite(position)) return false;
            foreach (var arrow in session.Puzzle.Arrows)
            {
                if (!layout.GetCellRect(arrow.Key).Contains(position)) continue;
                if (canBegin != null && !canBegin(arrow.Key)) return false;
                var now = clock();
                if (!double.IsFinite(now)) return false;
                Origin = arrow.Key;
                Path = Array.AsReadOnly(DirectionGeometry.GetDirectionalSegment(layout.Size, Origin, arrow.Value).ToArray());
                pressPosition = position; startedAt = now;
                ActivePointerId = pointerId; HasActivePointer = true; IsHighlighted = false;
                return true;
            }
            return false;
        }

        public void Move(int pointerId, Vector2 position)
        {
            if (!HasActivePointer || pointerId != ActivePointerId) return;
            if (session.IsCompleted || !Finite(position) || !layout.GetCellRect(Origin).Contains(position) ||
                (position - pressPosition).sqrMagnitude >= tolerance * tolerance) Cancel();
        }

        // Fake clocks exercise boundaries without sleeping; UI schedules this only during a hold.
        public bool Tick()
        {
            if (!HasActivePointer) return false;
            if (session.IsCompleted) { Cancel(); return false; }
            var now = clock();
            if (!double.IsFinite(now) || now < startedAt) { Cancel(); return false; }
            if (IsHighlighted || now - startedAt < HoldSeconds) return false;
            IsHighlighted = true;
            activated?.Invoke(Origin);
            return true;
        }

        public void End(int pointerId)
        {
            if (HasActivePointer && pointerId == ActivePointerId) Cancel();
        }
        public void Cancel()
        {
            HasActivePointer = false; ActivePointerId = -1; IsHighlighted = false;
            Path = Array.Empty<BoardCoordinate>();
        }
        private static bool Finite(Vector2 position) => float.IsFinite(position.x) && float.IsFinite(position.y);
    }
}
