using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #76: vector gear shares the menu ink and avoids platform glyph fallback.
    internal sealed class SettingsGearElement : VisualElement
    {
        public SettingsGearElement()
        {
            name = "settings-gear"; pickingMode = PickingMode.Ignore;
            style.width = style.height = 26;
            generateVisualContent += Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var center = contentRect.center;
            var radius = Mathf.Min(contentRect.width, contentRect.height) * .47f;
            painter.fillColor = BoardSymbolElement.Ink;
            painter.BeginPath();
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI / 16;
                var r = radius * (i % 4 == 0 || i % 4 == 3 ? 1 : .78f);
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
            }
            painter.ClosePath(); painter.Fill();
            painter.fillColor = PortraitBoardView.Background;
            painter.BeginPath(); painter.Arc(center, radius * .36f, 0, 360); painter.Fill();
        }
    }
}
