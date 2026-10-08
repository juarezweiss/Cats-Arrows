using System;
using CatsAndArrows.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Replaceable vector artwork; no glyph dependencies or puzzle-state ownership.
    public sealed class BoardSymbolElement : VisualElement
    {
        public static readonly Color Ink = new Color(.18f, .22f, .20f);
        private BoardCellKind kind;
        private Direction direction;
        public BoardCellKind Kind { get => kind; set { kind = value; MarkDirtyRepaint(); } }
        public Direction Direction { get => direction; set { direction = value; MarkDirtyRepaint(); } }
        public BoardSymbolElement(BoardCellKind kind = BoardCellKind.Empty, Direction direction = Direction.Up)
        {
            this.kind = kind; this.direction = direction;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }
        public static Vector2 ArrowVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Vector2.up * -1;
                case Direction.Down: return Vector2.up;
                case Direction.Left: return Vector2.left;
                case Direction.Right: return Vector2.right;
                case Direction.UpLeft: return new Vector2(-1, -1).normalized;
                case Direction.UpRight: return new Vector2(1, -1).normalized;
                case Direction.DownLeft: return new Vector2(-1, 1).normalized;
                case Direction.DownRight: return new Vector2(1, 1).normalized;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
        private void Draw(MeshGenerationContext context)
        {
            var size = Mathf.Min(contentRect.width, contentRect.height);
            if (size <= 0) return;
            var center = contentRect.center;
            var painter = context.painter2D;
            painter.strokeColor = Ink; painter.fillColor = Ink;
            painter.lineWidth = Mathf.Max(2, size * .055f);
            painter.lineCap = LineCap.Round; painter.lineJoin = LineJoin.Round;
            Vector2 Point(float x, float y) => center + new Vector2(x, y) * size;
            void Line(Vector2 start, Vector2 end)
            { painter.BeginPath(); painter.MoveTo(start); painter.LineTo(end); painter.Stroke(); }
            switch (Kind)
            {
                case BoardCellKind.Empty: return;
                case BoardCellKind.X:
                    Line(Point(-.19f, -.19f), Point(.19f, .19f));
                    Line(Point(.19f, -.19f), Point(-.19f, .19f));
                    break;
                case BoardCellKind.Arrow:
                    var vector = ArrowVector(Direction);
                    var side = new Vector2(-vector.y, vector.x);
                    var tip = center + vector * size * .25f;
                    Line(center - vector * size * .25f, tip);
                    Line(tip - vector * size * .16f + side * size * .15f, tip);
                    Line(tip, tip - vector * size * .16f - side * size * .15f);
                    break;
                case BoardCellKind.Cat:
                    painter.BeginPath(); painter.MoveTo(Point(-.26f, -.27f));
                    painter.LineTo(Point(-.10f, -.15f));
                    painter.BezierCurveTo(Point(-.04f, -.18f), Point(.04f, -.18f), Point(.10f, -.15f));
                    painter.LineTo(Point(.26f, -.27f)); painter.LineTo(Point(.27f, .06f));
                    painter.BezierCurveTo(Point(.27f, .32f), Point(-.27f, .32f), Point(-.27f, .06f));
                    painter.ClosePath(); painter.Fill();
                    painter.strokeColor = new Color(.98f, .96f, .91f);
                    painter.lineWidth = Mathf.Max(1.8f, size * .045f);
                    Line(Point(-.13f, .02f), Point(-.10f, .04f));
                    Line(Point(.10f, .04f), Point(.13f, .02f));
                    Line(Point(-.035f, .13f), Point(.035f, .13f));
                    break;
                case BoardCellKind.Protected:
                    painter.lineWidth = Mathf.Max(1.5f, size * .036f);
                    painter.BeginPath(); painter.MoveTo(Point(-.24f, -.15f)); painter.LineTo(Point(-.15f, -.24f));
                    painter.LineTo(Point(.15f, -.24f)); painter.LineTo(Point(.24f, -.15f));
                    painter.LineTo(Point(.24f, .15f)); painter.LineTo(Point(.15f, .24f));
                    painter.LineTo(Point(-.15f, .24f)); painter.LineTo(Point(-.24f, .15f));
                    painter.ClosePath(); painter.Stroke();
                    Line(Point(-.16f, .04f), Point(.04f, -.16f));
                    Line(Point(-.10f, .16f), Point(.16f, -.10f));
                    Line(Point(.05f, .16f), Point(.16f, .05f));
                    break;
            }
        }
    }
}
