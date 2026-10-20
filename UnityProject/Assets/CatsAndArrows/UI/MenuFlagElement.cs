using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #72: small vector flags avoid platform-dependent emoji/font fallback.
    internal sealed class MenuFlagElement : VisualElement
    {
        private readonly string locale;
        public MenuFlagElement(string locale)
        {
            this.locale = locale;
            pickingMode = PickingMode.Ignore;
            style.width = 30; style.height = 20; style.flexShrink = 0;
            generateVisualContent += Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            Vector2 Point(float x, float y) => new Vector2(x * contentRect.width, y * contentRect.height);
            void Polygon(Color color, params Vector2[] points)
            {
                p.fillColor = color; p.BeginPath(); p.MoveTo(points[0]);
                for (var i = 1; i < points.Length; i++) p.LineTo(points[i]);
                p.ClosePath(); p.Fill();
            }
            void Box(Color color, float x, float y, float w, float h) =>
                Polygon(color, Point(x, y), Point(x + w, y), Point(x + w, y + h), Point(x, y + h));
            void Circle(Color color, float x, float y, float radius)
            {
                p.fillColor = color; p.BeginPath();
                p.Arc(Point(x, y), radius * contentRect.height, 0, 360); p.Fill();
            }
            var red = new Color(.72f, .10f, .17f);
            var blue = new Color(.07f, .18f, .39f);
            var gold = new Color(1, .81f, .22f);
            if (locale == "pt-BR")
            {
                Box(new Color(.08f, .49f, .30f), 0, 0, 1, 1);
                Polygon(gold, Point(.5f, .10f), Point(.91f, .5f), Point(.5f, .9f), Point(.09f, .5f));
                Circle(blue, .5f, .5f, .24f);
                Polygon(Color.white, Point(.35f, .39f), Point(.47f, .42f), Point(.65f, .54f),
                    Point(.65f, .60f), Point(.46f, .48f), Point(.35f, .45f));
                Circle(Color.white, .48f, .60f, .018f);
                Circle(Color.white, .55f, .65f, .018f);
            }
            else if (locale == "es")
            {
                Box(red, 0, 0, 1, 1); Box(gold, 0, .25f, 1, .5f);
                // Compact heraldic silhouette within the yellow band.
                Box(red, .26f, .42f, .12f, .22f);
                Box(red, .25f, .35f, .03f, .10f); Box(red, .305f, .33f, .03f, .12f);
                Box(red, .36f, .35f, .03f, .10f);
                Box(gold, .31f, .53f, .025f, .11f);
            }
            else
            {
                Box(blue, 0, 0, 1, 1);
                Polygon(Color.white, Point(0, 0), Point(.12f, 0), Point(1, .88f), Point(1, 1), Point(.88f, 1), Point(0, .12f));
                Polygon(Color.white, Point(1, 0), Point(.88f, 0), Point(0, .88f), Point(0, 1), Point(.12f, 1), Point(1, .12f));
                Polygon(red, Point(0, 0), Point(.05f, 0), Point(1, .95f), Point(1, 1), Point(.95f, 1), Point(0, .05f));
                Polygon(red, Point(1, 0), Point(.95f, 0), Point(0, .95f), Point(0, 1), Point(.05f, 1), Point(1, .05f));
                Box(Color.white, .39f, 0, .22f, 1); Box(Color.white, 0, .33f, 1, .34f);
                Box(red, .44f, 0, .12f, 1); Box(red, 0, .40f, 1, .20f);
            }
        }
    }
}
