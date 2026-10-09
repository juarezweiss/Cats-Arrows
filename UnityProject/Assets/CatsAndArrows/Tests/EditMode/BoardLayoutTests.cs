using CatsAndArrows.Core;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;

namespace CatsAndArrows.Tests
{
    // Issue #6: geometry checks cover narrow portrait, cutouts, and future grid sizes.
    public sealed class BoardLayoutTests
    {
        // Issue #19: center playable tiles rather than the asymmetric clue+tile block.
        [TestCase(288, 232, 5)] // 320 × 568, simulated insets, frame/header/controls reserved.
        [TestCase(358, 508, 5)]
        [TestCase(288, 80, 5)] // Height-limited layout must still retain visible clues.
        [TestCase(358, 508, 7)]
        [TestCase(358, 508, 10)]
        public void TileGridIsCenteredWithEveryVisibleClueInsideAvailableRegion(float width, float height, int size)
        {
            var layout = BoardLayout.Calculate(width, height, size);
            var origin = new Vector2((width - layout.GridSize) / 2 - layout.ClueGutter,
                (height - layout.GridSize) / 2 - layout.ClueGutter);
            var first = layout.GetCellRect(new BoardCoordinate(0, 0));
            var last = layout.GetCellRect(new BoardCoordinate(size - 1, size - 1));
            Assert.That(origin.x + (first.xMin + last.xMax) / 2, Is.EqualTo(width / 2).Within(.001f));
            Assert.That(origin.y + (first.yMin + last.yMax) / 2, Is.EqualTo(height / 2).Within(.001f));
            Assert.That(layout.CellSize, Is.GreaterThan(0));
            for (var index = 0; index < size; index++)
            {
                foreach (var rect in new[] { layout.GetRowClueRect(index), layout.GetColumnClueRect(index),
                    layout.GetCellRect(new BoardCoordinate(index, index)) })
                    AssertInside(new Rect(rect.position + origin, rect.size), width, height);
            }
        }

        [TestCase(320, 568, 20, 20, 5)]
        [TestCase(390, 844, 34, 47, 5)]
        [TestCase(393, 852, 34, 59, 5)]
        [TestCase(320, 568, 20, 20, 7)]
        [TestCase(390, 844, 34, 47, 7)]
        [TestCase(393, 852, 34, 59, 10)]
        public void PortraitBoardsAndCluesFitSafeAreaWithAlignedSquareCells(
            int screenWidth, int screenHeight, int bottomInset, int topInset, int size)
        {
            var safe = SafeAreaLayout.ToPanelRect(
                new Rect(0, bottomInset, screenWidth, screenHeight - bottomInset - topInset),
                new Vector2(screenWidth, screenHeight), new Vector2(screenWidth, screenHeight));
            var layout = BoardLayout.Calculate(safe.width, safe.height, size);
            Assert.That(safe.y, Is.EqualTo(topInset).Within(0.001f));
            Assert.That(safe.yMax, Is.EqualTo(screenHeight - bottomInset).Within(0.001f));
            Assert.That(layout.Size, Is.EqualTo(size));
            Assert.That(layout.CellSize, Is.GreaterThan(0));
            Assert.That(layout.Width, Is.LessThanOrEqualTo(safe.width + 0.001f));
            Assert.That(layout.Height, Is.LessThanOrEqualTo(safe.height + 0.001f));
            for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
            {
                var cell = layout.GetCellRect(new BoardCoordinate(row, column));
                var rowClue = layout.GetRowClueRect(row);
                var columnClue = layout.GetColumnClueRect(column);
                AssertInside(cell, layout.Width, layout.Height);
                AssertInside(rowClue, layout.Width, layout.Height);
                AssertInside(columnClue, layout.Width, layout.Height);
                Assert.That(cell.width, Is.EqualTo(layout.CellSize).Within(0.001f));
                Assert.That(cell.height, Is.EqualTo(layout.CellSize).Within(0.001f));
                Assert.That(rowClue.center.y, Is.EqualTo(cell.center.y).Within(0.001f));
                Assert.That(columnClue.center.x, Is.EqualTo(cell.center.x).Within(0.001f));
                Assert.That(rowClue.xMax, Is.LessThanOrEqualTo(cell.xMin + 0.001f));
                Assert.That(columnClue.yMax, Is.LessThanOrEqualTo(cell.yMin + 0.001f));
                if (column + 1 < size)
                    Assert.That(layout.GetCellRect(new BoardCoordinate(row, column + 1)).xMin,
                        Is.GreaterThanOrEqualTo(cell.xMax - 0.001f));
                if (row + 1 < size)
                    Assert.That(layout.GetCellRect(new BoardCoordinate(row + 1, column)).yMin,
                        Is.GreaterThanOrEqualTo(cell.yMax - 0.001f));
            }
        }

        [Test]
        public void SafeAreaConversionScalesPixelsAndFlipsToPanelTopLeftCoordinates()
        {
            var result = SafeAreaLayout.ToPanelRect(new Rect(60, 100, 960, 1700),
                new Vector2(1080, 1920), new Vector2(360, 640));
            Assert.That(result.x, Is.EqualTo(20).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(40).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(320).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(1700f / 3).Within(0.001f));
        }

        [Test]
        public void SafeAreaConversionClampsOffScreenReportedBounds()
        {
            var result = SafeAreaLayout.ToPanelRect(new Rect(-20, -30, 430, 920),
                new Vector2(390, 844), new Vector2(390, 844));
            Assert.That(result, Is.EqualTo(new Rect(0, 0, 390, 844)));
        }

        [TestCase(Direction.Up, 0, -1)]
        [TestCase(Direction.Down, 0, 1)]
        [TestCase(Direction.Left, -1, 0)]
        [TestCase(Direction.Right, 1, 0)]
        [TestCase(Direction.UpLeft, -1, -1)]
        [TestCase(Direction.UpRight, 1, -1)]
        [TestCase(Direction.DownLeft, -1, 1)]
        [TestCase(Direction.DownRight, 1, 1)]
        public void ArrowArtworkPointsAlongBoardRowsAndColumns(Direction direction, int x, int y)
        {
            var vector = BoardSymbolElement.ArrowVector(direction);
            Assert.That(vector.normalized.x, Is.EqualTo(new Vector2(x, y).normalized.x).Within(0.001f));
            Assert.That(vector.normalized.y, Is.EqualTo(new Vector2(x, y).normalized.y).Within(0.001f));
        }

        private static void AssertInside(Rect rect, float width, float height)
        {
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width + 0.001f));
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height + 0.001f));
        }
    }
}
