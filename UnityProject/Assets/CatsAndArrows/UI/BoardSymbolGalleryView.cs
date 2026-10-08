using System;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Isolated artwork review: these symbols never enter the generated game session.
    public sealed class BoardSymbolGalleryView : VisualElement
    {
        public BoardSymbolGalleryView(string locale)
        {
            var text = new LocalizationService(BoardLocalization.CreateCatalog()); text.SelectLanguage(locale);
            style.flexGrow = 1; style.paddingLeft = 24; style.paddingRight = 24; style.paddingTop = 64;
            style.backgroundColor = PortraitBoardView.Background; style.color = BoardSymbolElement.Ink;
            style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var heading = new Label(text.GetText("board.review") + " · " + text.GetText("board.symbols"));
            heading.style.fontSize = 22; heading.style.whiteSpace = WhiteSpace.Normal; heading.style.marginBottom = 24; Add(heading);
            var states = new VisualElement(); states.style.flexDirection = FlexDirection.Row;
            states.style.flexWrap = Wrap.Wrap; Add(states);
            foreach (BoardCellKind kind in Enum.GetValues(typeof(BoardCellKind)))
                states.Add(Item(kind, Direction.UpRight, text.GetText("board." + kind.ToString().ToLowerInvariant()), 50));
            var arrows = new VisualElement(); arrows.style.flexDirection = FlexDirection.Row;
            arrows.style.flexWrap = Wrap.Wrap; arrows.style.marginTop = 28; Add(arrows);
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                arrows.Add(Item(BoardCellKind.Arrow, direction, text.GetText("direction." + direction.ToString().ToLowerInvariant()), 50));
        }
        private static VisualElement Item(BoardCellKind kind, Direction direction, string label, float widthPercent)
        {
            var item = new VisualElement(); item.style.width = Length.Percent(widthPercent); item.style.alignItems = Align.Center;
            item.style.marginBottom = 16;
            var symbol = new BoardSymbolElement(kind, direction); symbol.style.width = 44; symbol.style.height = 44;
            symbol.style.backgroundColor = PortraitBoardView.Tile;
            var text = new Label(label); text.style.fontSize = 12; text.style.whiteSpace = WhiteSpace.Normal;
            text.style.unityTextAlign = TextAnchor.UpperCenter; text.style.alignSelf = Align.Stretch;
            item.Add(symbol); item.Add(text); return item;
        }
    }
}
