using System;
using System.Collections.Generic;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using UnityEngine;

namespace CatsAndArrows.UI
{
    public enum BoardDragAxis { None, Horizontal, Vertical }

    // Issue #7: pointer-independent intent recognition. Positions use board-local units.
    // GameSession remains the only authority for marks and completion.
    public sealed class BoardGestureController
    {
        private readonly GameSession session;
        private readonly BoardLayout layout;
        private readonly float threshold;
        private readonly HashSet<BoardCoordinate> visited = new HashSet<BoardCoordinate>();
        private BoardCoordinate origin;
        private Vector2 pressPosition;
        private Vector2 previousPosition;
        private bool canStartDrag;
        public bool HasActivePointer { get; private set; }
        public int ActivePointerId { get; private set; } = -1;
        public bool IsDragging => Axis != BoardDragAxis.None;
        public BoardDragAxis Axis { get; private set; }

        public BoardGestureController(GameSession session, BoardLayout layout, float dragThreshold = 8f)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (layout.Size != session.Puzzle.Size || layout.CellSize <= 0)
                throw new ArgumentException("Layout must match the session and have positive cells.", nameof(layout));
            if (!float.IsFinite(dragThreshold) || dragThreshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(dragThreshold));
            this.layout = layout;
            threshold = dragThreshold;
        }

        public bool Begin(int pointerId, Vector2 position)
        {
            if (HasActivePointer || !TryGetCell(position, out var coordinate) || !session.CanInteract(coordinate)) return false;
            HasActivePointer = true;
            ActivePointerId = pointerId;
            origin = coordinate;
            pressPosition = previousPosition = position;
            canStartDrag = session.GetMark(origin) == PlayerMark.Empty;
            Axis = BoardDragAxis.None;
            visited.Clear();
            return true;
        }

        public bool Move(int pointerId, Vector2 position)
        {
            if (!HasActivePointer || pointerId != ActivePointerId) return false;
            if (session.IsCompleted || !InsideGrid(position)) { Cancel(); return false; }
            if (!IsDragging)
            {
                var displacement = position - pressPosition;
                if (displacement.sqrMagnitude < threshold * threshold) return false;
                // Dragging a pre-existing mark must not turn release into a tap.
                if (!canStartDrag) { Cancel(); return false; }
                Axis = Mathf.Abs(displacement.x) >= Mathf.Abs(displacement.y)
                    ? BoardDragAxis.Horizontal : BoardDragAxis.Vertical;
            }
            var from = Axis == BoardDragAxis.Horizontal ? previousPosition.x : previousPosition.y;
            var to = Axis == BoardDragAxis.Horizontal ? position.x : position.y;
            var low = Mathf.Min(from, to);
            var high = Mathf.Max(from, to);
            var coordinates = new List<BoardCoordinate>();
            // Intersect the swept axis with tile extents: skipped frames bridge cells,
            // while landing in a gap cannot mark a tile the pointer has not reached.
            for (var index = 0; index < layout.Size; index++)
            {
                var coordinate = Axis == BoardDragAxis.Horizontal
                    ? new BoardCoordinate(origin.Row, index) : new BoardCoordinate(index, origin.Column);
                var rect = layout.GetCellRect(coordinate);
                var start = Axis == BoardDragAxis.Horizontal ? rect.xMin : rect.yMin;
                var end = Axis == BoardDragAxis.Horizontal ? rect.xMax : rect.yMax;
                if (high >= start && low < end && visited.Add(coordinate)) coordinates.Add(coordinate);
            }
            previousPosition = position;
            return session.ApplyDrag(coordinates) > 0;
        }

        public bool End(int pointerId, Vector2 position)
        {
            if (!HasActivePointer || pointerId != ActivePointerId) return false;
            var changed = Move(pointerId, position);
            if (!HasActivePointer) return changed;
            if (!IsDragging && TryGetCell(position, out var coordinate) && coordinate.Equals(origin))
                changed |= session.Tap(origin);
            Cancel();
            return changed;
        }

        public void Cancel()
        {
            HasActivePointer = false;
            ActivePointerId = -1;
            Axis = BoardDragAxis.None;
            visited.Clear();
        }

        public bool TryGetCell(Vector2 position, out BoardCoordinate coordinate)
        {
            coordinate = default;
            if (!InsideGrid(position)) return false;
            // Use the exact rendered boundaries. Inverting a fractional pitch with
            // FloorToInt can round a tile's inclusive minimum into the prior gap.
            for (var row = 0; row < layout.Size; row++)
            {
                var rowRect = layout.GetCellRect(new BoardCoordinate(row, 0));
                if (position.y < rowRect.yMin || position.y >= rowRect.yMax) continue;
                for (var column = 0; column < layout.Size; column++)
                {
                    var candidate = new BoardCoordinate(row, column);
                    if (!layout.GetCellRect(candidate).Contains(position)) continue;
                    coordinate = candidate;
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool InsideGrid(Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)) return false;
            var first = layout.GetCellRect(new BoardCoordinate(0, 0));
            var last = layout.GetCellRect(new BoardCoordinate(layout.Size - 1, layout.Size - 1));
            return position.x >= first.xMin && position.x < last.xMax &&
                position.y >= first.yMin && position.y < last.yMax;
        }
    }
}
