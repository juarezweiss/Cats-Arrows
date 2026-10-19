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
                    // Issue #53: a lighter paw, about one third smaller and centered in the cell.
                    size *= .69f;
                    center.y += size * .0225f;
                    painter.BeginPath(); painter.MoveTo(Point(0, -.025f));
                    painter.BezierCurveTo(Point(.09f, -.025f), Point(.105f, .065f), Point(.17f, .13f));
                    painter.BezierCurveTo(Point(.25f, .24f), Point(.13f, .28f), Point(0, .28f));
                    painter.BezierCurveTo(Point(-.13f, .28f), Point(-.25f, .24f), Point(-.17f, .13f));
                    painter.BezierCurveTo(Point(-.105f, .065f), Point(-.09f, -.025f), Point(0, -.025f));
                    painter.ClosePath(); painter.Fill();
                    Toe(-.24f, -.10f, .07f, .08f);
                    Toe(-.095f, -.245f, .07f, .08f);
                    Toe(.095f, -.245f, .07f, .08f);
                    Toe(.24f, -.10f, .07f, .08f);
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
            void Toe(float x, float y, float radiusX, float radiusY)
            {
                const float curve = .55228475f;
                painter.BeginPath(); painter.MoveTo(Point(x + radiusX, y));
                painter.BezierCurveTo(Point(x + radiusX, y + curve * radiusY), Point(x + curve * radiusX, y + radiusY), Point(x, y + radiusY));
                painter.BezierCurveTo(Point(x - curve * radiusX, y + radiusY), Point(x - radiusX, y + curve * radiusY), Point(x - radiusX, y));
                painter.BezierCurveTo(Point(x - radiusX, y - curve * radiusY), Point(x - curve * radiusX, y - radiusY), Point(x, y - radiusY));
                painter.BezierCurveTo(Point(x + curve * radiusX, y - radiusY), Point(x + radiusX, y - curve * radiusY), Point(x + radiusX, y));
                painter.ClosePath(); painter.Fill();
            }
        }
    }
}
