using System;
using System.Collections.Generic;
using System.Globalization;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Presentation and a single pointer adapter; GameSession owns every transition.
    public sealed class PortraitBoardView : VisualElement
    {
        public static readonly Color Background = new Color(245 / 255f, 242 / 255f, 233 / 255f);
        public static readonly Color Tile = new Color(1, 253 / 255f, 247 / 255f);
        private static readonly Color Border = new Color(126 / 255f, 140 / 255f, 131 / 255f);
        private static readonly Color Secondary = new Color(93 / 255f, 105 / 255f, 98 / 255f);
        private readonly VisualElement safeFrame = new VisualElement();
        private readonly VisualElement boardRegion = new VisualElement();
        private readonly VisualElement board = new VisualElement();
        private readonly VisualElement legend = new VisualElement();
        private readonly VisualElement header = new VisualElement();
        private readonly Label title = new Label();
        private readonly Label difficulty = new Label();
        private readonly Label intro = new Label();
        private readonly Label error = new Label();
        private GameSession session;
        private BoardGestureController gesture;
        private LocalizationService text;
        private bool interactionSuspended;
        public event Action StateChanged;
        public bool InteractionEnabled => !interactionSuspended;
        public BoardPresentation Presentation { get; private set; }
        public BoardLayout Layout { get; private set; }

        public PortraitBoardView()
        {
            name = "portrait-board-view";
            style.flexGrow = 1;
            style.backgroundColor = Background;
            style.color = BoardSymbolElement.Ink;
            style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            safeFrame.name = "safe-frame";
            safeFrame.style.position = Position.Absolute;
            Add(safeFrame);

            Absolute(header, new Rect(0, 0, 0, 104)); header.style.right = 0; header.style.width = StyleKeyword.Auto;
            header.style.alignItems = Align.Center;
            title.style.fontSize = 26; title.style.unityFontStyleAndWeight = FontStyle.Bold;
            difficulty.style.fontSize = 16; difficulty.style.marginTop = 8;
            intro.style.fontSize = 15; intro.style.marginTop = 6; intro.style.color = Secondary;
            intro.name = "board-status";
            foreach (var label in new[] { title, difficulty, intro })
            {
                label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0;
                label.style.marginLeft = label.style.marginRight = label.style.marginBottom = 0;
            }
            header.Add(title); header.Add(difficulty); header.Add(intro); safeFrame.Add(header);
            boardRegion.style.position = Position.Absolute;
            boardRegion.style.left = 0; boardRegion.style.right = 0; boardRegion.style.top = 104; boardRegion.style.bottom = 126;
            board.style.position = Position.Absolute;
            board.name = "board-grid";
            boardRegion.Add(board);
            error.style.whiteSpace = WhiteSpace.Normal; error.style.fontSize = 18;
            error.style.unityTextAlign = TextAnchor.MiddleCenter; error.style.flexGrow = 1;
            error.style.alignSelf = Align.Center; error.style.maxWidth = 340;
            boardRegion.Add(error); safeFrame.Add(boardRegion);
            legend.style.position = Position.Absolute;
            legend.style.left = 0; legend.style.right = 0; legend.style.bottom = 0; legend.style.height = 112;
            safeFrame.Add(legend);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateSafeArea());
            boardRegion.RegisterCallback<GeometryChangedEvent>(_ => LayoutBoard());
            board.RegisterCallback<PointerDownEvent>(OnPointerDown);
            board.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            board.RegisterCallback<PointerUpEvent>(OnPointerUp);
            board.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            board.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            board.RegisterCallback<GeometryChangedEvent>(_ => CancelInteraction());
            RegisterCallback<DetachFromPanelEvent>(_ => CancelInteraction());
        }

        public void SetPlayLayout()
        {
            header.style.height = 48;
            title.style.fontSize = 20;
            difficulty.style.fontSize = 14; difficulty.style.marginTop = 2;
            intro.style.display = DisplayStyle.None;
            boardRegion.style.top = 56; boardRegion.style.bottom = 120;
            legend.style.display = DisplayStyle.None;
            LayoutBoard();
        }
        public void SetInteractionSuspended(bool suspended)
        {
            CancelInteraction();
            interactionSuspended = suspended;
        }
        public void HighlightCell(BoardCoordinate coordinate)
        {
            var tile = board.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column);
            if (tile == null) return;
            tile.style.borderLeftWidth = tile.style.borderRightWidth = tile.style.borderTopWidth = tile.style.borderBottomWidth = 3;
            tile.style.borderLeftColor = tile.style.borderRightColor = tile.style.borderTopColor = tile.style.borderBottomColor = BoardSymbolElement.Ink;
        }
        public void Bind(GameSession newSession, string locale = "en")
        {
            if (newSession == null) throw new ArgumentNullException(nameof(newSession));
            CancelInteraction();
            gesture = null;
            session = newSession;
            SetLocale(locale);
            error.style.display = DisplayStyle.None;
            board.style.display = DisplayStyle.Flex;
            Refresh();
        }
        public void Refresh()
        {
            if (session == null) return;
            if (session.IsCompleted) CancelInteraction();
            Presentation = BoardPresentation.FromSession(session);
            board.Clear();
            intro.text = text.GetText(Presentation.IsCompleted ? "board.completed" : "board.intro");
            for (var row = 0; row < Presentation.Size; row++)
            {
                var rowLabel = Clue(Presentation.RowTotals[row]); rowLabel.name = "row-clue-" + row;
                rowLabel.tooltip = text.Format("board.row-total", Values("index", row + 1, "total", Presentation.RowTotals[row]));
                board.Add(rowLabel);
                var columnLabel = Clue(Presentation.ColumnTotals[row]); columnLabel.name = "column-clue-" + row;
                columnLabel.tooltip = text.Format("board.column-total", Values("index", row + 1, "total", Presentation.ColumnTotals[row]));
                board.Add(columnLabel);
            }
            foreach (var cell in Presentation.Cells)
            {
                var tile = new VisualElement { name = "cell-" + cell.Coordinate.Row + "-" + cell.Coordinate.Column };
                tile.style.position = Position.Absolute;
                tile.style.backgroundColor = Tile;
                tile.style.borderLeftWidth = tile.style.borderRightWidth = tile.style.borderTopWidth = tile.style.borderBottomWidth = 1;
                tile.style.borderLeftColor = tile.style.borderRightColor = tile.style.borderTopColor = tile.style.borderBottomColor = Border;
                tile.style.borderTopLeftRadius = tile.style.borderTopRightRadius = tile.style.borderBottomLeftRadius = tile.style.borderBottomRightRadius = 7;
                var symbol = new BoardSymbolElement(cell.Kind, cell.Direction); symbol.style.flexGrow = 1; tile.Add(symbol);
                var state = text.GetText("board." + cell.Kind.ToString().ToLowerInvariant());
                if (cell.Kind == BoardCellKind.Arrow)
                    state = text.Format("board.arrow-description", new Dictionary<string, object> { { "direction", text.GetText("direction." + cell.Direction.ToString().ToLowerInvariant()) } });
                tile.tooltip = text.Format("board.cell-description", new Dictionary<string, object>
                    { { "row", cell.Coordinate.Row + 1 }, { "column", cell.Coordinate.Column + 1 }, { "state", state } });
                // Display metadata only; pointer recognition uses shared board geometry.
                tile.userData = cell;
                board.Add(tile);
            }
            LayoutBoard();
        }
        public void ShowGenerationFailure(GenerationStatus generationStatus, string locale = "en")
        {
            if (generationStatus == GenerationStatus.Success) throw new ArgumentException("A failure status is required.", nameof(generationStatus));
            CancelInteraction(); gesture = null;
            session = null; Presentation = null; board.Clear(); SetLocale(locale);
            board.style.display = DisplayStyle.None;
            error.style.display = DisplayStyle.Flex;
            error.text = new GenerationLocalizer(text).Format(generationStatus);
        }
        public void ApplySafeArea(Rect pixelSafeArea, Vector2 screenPixels)
        {
            var size = new Vector2(contentRect.width, contentRect.height);
            if (!float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0 || size.y <= 0) return;
            var safe = SafeAreaLayout.ToPanelRect(pixelSafeArea, screenPixels, size);
            // The view occupies the full-screen panel. Insets are converted into its actual units.
            Absolute(safeFrame, new Rect(safe.x + 16, safe.y + 16, Mathf.Max(0, safe.width - 32), Mathf.Max(0, safe.height - 32)));
        }
        public void UpdateSafeArea()
        {
            if (Screen.width > 0 && Screen.height > 0) ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }
        private void SetLocale(string locale)
        {
            text = new LocalizationService(BoardLocalization.CreateCatalog(), CultureInfo.GetCultureInfo("en"));
            text.SelectLanguage(locale);
            title.text = text.GetText("board.title"); difficulty.text = text.GetText("board.easy"); intro.text = text.GetText("board.intro");
            BuildLegend();
        }
        private void BuildLegend()
        {
            legend.Clear();
            var heading = new Label(text.GetText("board.symbols")); heading.style.fontSize = 12; heading.style.color = Secondary;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter; heading.style.marginBottom = 10; legend.Add(heading);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.SpaceAround; legend.Add(row);
            foreach (BoardCellKind kind in Enum.GetValues(typeof(BoardCellKind)))
            {
                var item = new VisualElement(); item.style.width = Length.Percent(20); item.style.alignItems = Align.Center;
                var swatch = new VisualElement(); swatch.style.width = 38; swatch.style.height = 38;
                swatch.style.backgroundColor = Tile;
                swatch.style.borderLeftWidth = swatch.style.borderRightWidth = swatch.style.borderTopWidth = swatch.style.borderBottomWidth = 1;
                swatch.style.borderLeftColor = swatch.style.borderRightColor = swatch.style.borderTopColor = swatch.style.borderBottomColor = Border;
                var symbol = new BoardSymbolElement(kind, Direction.UpRight); symbol.style.flexGrow = 1; swatch.Add(symbol); item.Add(swatch);
                var label = new Label(text.GetText("board." + kind.ToString().ToLowerInvariant()));
                label.style.fontSize = 11; label.style.marginTop = 7; label.style.whiteSpace = WhiteSpace.Normal;
                label.style.unityTextAlign = TextAnchor.UpperCenter; label.style.alignSelf = Align.Stretch;
                item.Add(label); row.Add(item);
            }
        }
        private void LayoutBoard()
        {
            if (Presentation == null || !float.IsFinite(boardRegion.contentRect.width) ||
                !float.IsFinite(boardRegion.contentRect.height) ||
                boardRegion.contentRect.width <= 0 || boardRegion.contentRect.height <= 0) return;
            var nextLayout = BoardLayout.Calculate(boardRegion.contentRect.width, boardRegion.contentRect.height, Presentation.Size);
            if (gesture == null || Layout.Size != nextLayout.Size || Layout.CellSize != nextLayout.CellSize ||
                Layout.Gap != nextLayout.Gap || Layout.ClueSize != nextLayout.ClueSize)
            {
                CancelInteraction();
                gesture = new BoardGestureController(session, nextLayout);
            }
            Layout = nextLayout;
            Absolute(board, new Rect((boardRegion.contentRect.width - Layout.Width) / 2,
                (boardRegion.contentRect.height - Layout.Height) / 2, Layout.Width, Layout.Height));
            for (var line = 0; line < Presentation.Size; line++)
            {
                var row = board.Q<Label>("row-clue-" + line); var column = board.Q<Label>("column-clue-" + line);
                Absolute(row, Layout.GetRowClueRect(line)); Absolute(column, Layout.GetColumnClueRect(line));
                row.style.fontSize = column.style.fontSize = Mathf.Min(22, Layout.CellSize * .5f);
            }
            foreach (var cell in Presentation.Cells)
                Absolute(board.Q<VisualElement>("cell-" + cell.Coordinate.Row + "-" + cell.Coordinate.Column), Layout.GetCellRect(cell.Coordinate));
        }
        public void CancelInteraction()
        {
            if (gesture == null || !gesture.HasActivePointer) return;
            var pointerId = gesture.ActivePointerId;
            gesture.Cancel();
            if (board.HasPointerCapture(pointerId)) board.ReleasePointer(pointerId);
        }
        private Vector2 BoardPosition(Vector3 position) => board.WorldToLocal(new Vector2(position.x, position.y));
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (interactionSuspended || evt.button != 0 || gesture == null || !gesture.Begin(evt.pointerId, BoardPosition(evt.position))) return;
            // Capture belongs to the stable board, never a tile replaced by Refresh.
            board.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (gesture == null || !gesture.HasActivePointer || gesture.ActivePointerId != evt.pointerId) return;
            var changed = gesture.Move(evt.pointerId, BoardPosition(evt.position));
            FinishPointerEvent(evt.pointerId, changed);
            evt.StopPropagation();
        }
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0 || gesture == null || !gesture.HasActivePointer || gesture.ActivePointerId != evt.pointerId) return;
            var changed = gesture.End(evt.pointerId, BoardPosition(evt.position));
            FinishPointerEvent(evt.pointerId, changed);
            evt.StopPropagation();
        }
        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (gesture != null && gesture.HasActivePointer && gesture.ActivePointerId == evt.pointerId) CancelInteraction();
        }
        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (gesture != null && gesture.HasActivePointer && gesture.ActivePointerId == evt.pointerId) gesture.Cancel();
        }
        private void FinishPointerEvent(int pointerId, bool changed)
        {
            if (!gesture.HasActivePointer && board.HasPointerCapture(pointerId)) board.ReleasePointer(pointerId);
            if (changed) { Refresh(); StateChanged?.Invoke(); }
        }
        private Label Clue(int total)
        {
            var label = new Label(total.ToString(text.Culture)); label.style.position = Position.Absolute;
            label.style.unityTextAlign = TextAnchor.MiddleCenter; label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }
        private static Dictionary<string, object> Values(string key1, object value1, string key2, object value2) =>
            new Dictionary<string, object> { { key1, value1 }, { key2, value2 } };
        private static void Absolute(VisualElement element, Rect rect)
        {
            element.style.position = Position.Absolute; element.style.left = rect.x; element.style.top = rect.y;
            element.style.width = rect.width; element.style.height = rect.height;
        }
    }
}
