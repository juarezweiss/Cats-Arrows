using System;
using CatsAndArrows.Core;
using UnityEngine;

namespace CatsAndArrows.UI
{
    public readonly struct BoardLayout
    {
        public int Size { get; }
        public float CellSize { get; }
        public float Gap { get; }
        public float ClueSize { get; }
        public float ClueGutter => ClueSize + Gap;
        public float GridSize => Size * CellSize + (Size - 1) * Gap;
        public float Width { get; }
        public float Height => Width;
        private BoardLayout(int size, float cell, float gap, float clue)
        { Size = size; CellSize = cell; Gap = gap; ClueSize = clue; Width = clue + gap + size * cell + (size - 1) * gap; }
        public static BoardLayout Calculate(float availableWidth, float availableHeight, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (!float.IsFinite(availableWidth) || !float.IsFinite(availableHeight) || availableWidth <= 0 || availableHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(availableWidth));
            var extent = Mathf.Min(availableWidth, availableHeight, 520);
            var gap = Mathf.Min(4, extent / (size * 12f));
            var clue = Mathf.Min(32, extent / (size + 1));
            // Reserve the clue gutter on both sides so the tile grid itself can be
            // centered while the visible left/top clues remain inside the region.
            return new BoardLayout(size, Mathf.Min(64, Mathf.Max(0, (extent - 2 * (clue + gap) - (size - 1) * gap) / size)), gap, clue);
        }
        public Rect GetCellRect(BoardCoordinate coordinate)
        {
            Check(coordinate.Row); Check(coordinate.Column);
            return new Rect(ClueSize + Gap + coordinate.Column * (CellSize + Gap),
                ClueSize + Gap + coordinate.Row * (CellSize + Gap), CellSize, CellSize);
        }
        public Rect GetRowClueRect(int row)
        { Check(row); return new Rect(0, ClueSize + Gap + row * (CellSize + Gap), ClueSize, CellSize); }
        public Rect GetColumnClueRect(int column)
        { Check(column); return new Rect(ClueSize + Gap + column * (CellSize + Gap), 0, CellSize, ClueSize); }
        private void Check(int index) { if (index < 0 || index >= Size) throw new ArgumentOutOfRangeException(nameof(index)); }
    }

    public static class SafeAreaLayout
    {
        public static Rect ToPanelRect(Rect pixelSafeArea, Vector2 screenPixels, Vector2 panelSize)
        {
            if (screenPixels.x <= 0 || screenPixels.y <= 0 || panelSize.x <= 0 || panelSize.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenPixels));
            var left = Mathf.Clamp(pixelSafeArea.xMin, 0, screenPixels.x);
            var right = Mathf.Clamp(pixelSafeArea.xMax, left, screenPixels.x);
            var bottom = Mathf.Clamp(pixelSafeArea.yMin, 0, screenPixels.y);
            var top = Mathf.Clamp(pixelSafeArea.yMax, bottom, screenPixels.y);
            return new Rect(left / screenPixels.x * panelSize.x, (screenPixels.y - top) / screenPixels.y * panelSize.y,
                (right - left) / screenPixels.x * panelSize.x, (top - bottom) / screenPixels.y * panelSize.y);
        }
    }
}
