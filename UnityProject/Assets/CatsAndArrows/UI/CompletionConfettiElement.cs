using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // A fixed, mirrored burst: no particles, random state, allocations per frame or input ownership.
    public sealed class CompletionConfettiElement : VisualElement
    {
        public const int PieceCount = 40;
        private float elapsed;
        private static readonly Color[] Colors = {
            new Color(.46f, .59f, .50f), new Color(.76f, .57f, .40f),
            new Color(.56f, .56f, .69f), new Color(.77f, .66f, .40f)
        };

        public CompletionConfettiElement()
        {
            name = "completion-confetti";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.right = style.top = style.bottom = 0;
            style.overflow = Overflow.Hidden;
            generateVisualContent += Draw;
        }

        public void SetElapsed(double seconds)
        {
            elapsed = (float)seconds;
            style.display = seconds >= 0 && seconds < CompletionCelebration.ConfettiSeconds ? DisplayStyle.Flex : DisplayStyle.None;
            if (style.display.value == DisplayStyle.Flex) MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (elapsed < 0 || elapsed >= CompletionCelebration.ConfettiSeconds) return;
            var width = contentRect.width; var height = contentRect.height;
            if (width <= 0 || height <= 0) return;
            var painter = context.painter2D;
            for (var i = 0; i < PieceCount; i++)
            {
                var index = i / 2;
                var right = i % 2 == 1;
                var speed = width * (.18f + index % 7 * .022f);
                var x = speed * elapsed;
                if (right) x = width - x;
                var y = height * .57f - height * (.33f + index % 5 * .035f) * elapsed + height * .22f * elapsed * elapsed;
                var angle = index * .7f + elapsed * (1.1f + index % 3 * .4f);
                var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4;
                var side = new Vector2(-axis.y, axis.x) * .45f;
                var center = new Vector2(x, y);
                var color = Colors[index % Colors.Length];
                color.a = Mathf.Clamp01((3 - elapsed) * 2);
                painter.fillColor = color;
                painter.BeginPath(); painter.MoveTo(center - axis - side);
                painter.LineTo(center + axis - side); painter.LineTo(center + axis + side);
                painter.LineTo(center - axis + side); painter.ClosePath(); painter.Fill();
            }
        }
    }
}
