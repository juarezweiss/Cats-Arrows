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
        public static readonly Color ImmovableTile = new Color(231 / 255f, 229 / 255f, 220 / 255f);
        private static readonly Color CompletionTint = new Color(218 / 255f, 222 / 255f, 218 / 255f);
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
        private readonly VisualElement completionRow = new VisualElement();
        private readonly Label completionStatus = new Label();
        private readonly Button completionHint;
        private int highlightVersion;
        private BoardCoordinate? hintHighlight;
        private ArrowHoldController arrowHold;
        private readonly Func<double> holdClock;
        private IVisualElementScheduledItem holdTick;
        private Action reviewCompletionHint;
        private readonly Label error = new Label();
        private GameSession session;
        private TutorialController tutorial;
        private BoardGestureController gesture;
        private LocalizationService text;
        private bool interactionSuspended;
        public event Action StateChanged;
        public event Action Rebound;
        public event Action InteractionEnded;
        // Includes protected-cell attempts; separate from successful mark changes.
        public event Action ActivityChanged;
        private int activityPointerId = -1;
        public bool HasActiveInteraction => activityPointerId >= 0;
        public bool InteractionEnabled => !interactionSuspended && session != null && !session.IsCompleted;
        public bool IsBoundTo(GameSession candidate) => session != null && ReferenceEquals(session, candidate);
        public BoardPresentation Presentation { get; private set; }
        public BoardLayout Layout { get; private set; }

        public PortraitBoardView(Func<double> holdClock = null)
        {
            this.holdClock = holdClock ?? (() => Time.realtimeSinceStartupAsDouble);
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
            difficulty.name = "board-difficulty";
            difficulty.style.fontSize = 16; difficulty.style.marginTop = 8;
            intro.style.fontSize = 15; intro.style.marginTop = 6; intro.style.color = Secondary;
            intro.name = "board-status";
            foreach (var label in new[] { title, difficulty, intro })
            {
                label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0;
                label.style.marginLeft = label.style.marginRight = label.style.marginBottom = 0;
            }
            header.Add(title); header.Add(difficulty); header.Add(intro); safeFrame.Add(header);
            completionRow.style.flexDirection = FlexDirection.Row;
            completionRow.style.alignItems = Align.Center;
            completionRow.style.justifyContent = Justify.Center;
            completionRow.style.alignSelf = Align.Stretch;
            completionRow.style.height = 48;
            completionRow.style.minHeight = 48;
            completionRow.style.flexShrink = 0;
            completionRow.style.marginTop = 4;
            completionRow.style.display = DisplayStyle.None;
            completionStatus.name = "completion-status";
            completionStatus.enableRichText = true;
            completionStatus.style.fontSize = 14;
            completionStatus.style.whiteSpace = WhiteSpace.Normal;
            completionStatus.style.unityTextAlign = TextAnchor.MiddleCenter;
            completionStatus.style.flexShrink = 1;
            completionStatus.style.height = completionStatus.style.minHeight = 48;
            completionStatus.style.paddingLeft = completionStatus.style.paddingRight = 0;
            completionStatus.style.paddingTop = completionStatus.style.paddingBottom = 0;
            completionStatus.style.marginTop = completionStatus.style.marginBottom = 0;
            completionStatus.style.marginLeft = completionStatus.style.marginRight = 0;
            completionHint = new Button(() => reviewCompletionHint?.Invoke()) { name = "completion-hint" };
            completionHint.style.minHeight = 48;
            completionHint.style.width = 92;
            completionHint.style.flexShrink = 0;
            completionHint.style.fontSize = 14;
            completionHint.style.whiteSpace = WhiteSpace.Normal;
            completionHint.style.marginLeft = 8;
            completionHint.style.backgroundColor = Tile;
            completionHint.style.color = BoardSymbolElement.Ink;
            completionRow.Add(completionStatus); completionRow.Add(completionHint); header.Add(completionRow);
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
            // Reserve the same space before and after completion so the grid never
            // moves when the player places the final paw.
            header.style.height = 100;
            title.style.fontSize = 20;
            difficulty.style.fontSize = 14; difficulty.style.marginTop = 2;
            intro.style.display = DisplayStyle.None;
            boardRegion.style.top = 108; boardRegion.style.bottom = 120;
            legend.style.display = DisplayStyle.None;
            LayoutBoard();
        }
        public void SetCompletionFeedback(string status, string hintLabel = null, Action reviewHint = null)
        {
            completionStatus.text = status ?? "";
            completionRow.style.display = string.IsNullOrEmpty(status) ? DisplayStyle.None : DisplayStyle.Flex;
            completionHint.text = hintLabel ?? "";
            completionHint.style.display = reviewHint == null ? DisplayStyle.None : DisplayStyle.Flex;
            reviewCompletionHint = reviewHint;
        }
        public void SetCompletionOpacity(float opacity) => board.style.opacity = Mathf.Clamp01(opacity);
        public void SetTutorial(TutorialController controller)
        {
            CancelInteraction(); tutorial = controller; gesture = null;
        }
        public void SetTutorialFeedback(string instruction, bool expanded = false)
        {
            SetCompletionFeedback(instruction);
            completionStatus.style.fontSize = string.IsNullOrEmpty(instruction) ? 14 : 16;
            // The full goal needs three lines on compact phones. Use the clear
            // header-to-grid gap without moving the board or reducing its text.
            var height = !string.IsNullOrEmpty(instruction) && expanded ? 60 : 48;
            completionRow.style.height = completionRow.style.minHeight = height;
            completionStatus.style.height = completionStatus.style.minHeight = height;
        }
        public void SetInteractionSuspended(bool suspended)
        {
            CancelInteraction();
            interactionSuspended = suspended;
            ActivityChanged?.Invoke();
        }
        public void HighlightCell(BoardCoordinate coordinate, long durationMs = 0)
        {
            var previous = hintHighlight;
            hintHighlight = coordinate;
            var version = ++highlightVersion;
            if (previous.HasValue) RestoreCellOutline(previous.Value);
            RestoreCellOutline(coordinate);
            if (durationMs > 0) schedule.Execute(() =>
            {
                if (version != highlightVersion) return;
                hintHighlight = null;
                RestoreCellOutline(coordinate);
            }).StartingIn(durationMs);
        }

        public void Bind(GameSession newSession, string locale = "en")
        {
            if (newSession == null) throw new ArgumentNullException(nameof(newSession));
            Rebound?.Invoke();
            CancelInteraction();
            gesture = null; arrowHold = null;
            hintHighlight = null; ++highlightVersion;
            session = newSession;
            SetCompletionFeedback(null);
            SetLocale(locale);
            error.style.display = DisplayStyle.None;
            board.style.display = DisplayStyle.Flex;
            Refresh();
            ActivityChanged?.Invoke();
        }
        public void Refresh()
        {
            if (session == null) return;
            // Preserve the existing Hint lifecycle: rebuilding after a mark or
            // Clear removes its feedback. Hold release itself never rebuilds.
            hintHighlight = null; ++highlightVersion;
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
            foreach (var actualCell in Presentation.Cells)
            {
                // Intro examples are presentation-only. No paw/X intent is sent
                // to the session, so lesson demonstrations cannot win the board.
                var cell = actualCell;
                if (tutorial?.DemoCoordinate is BoardCoordinate demonstration && demonstration.Equals(actualCell.Coordinate) &&
                    tutorial.DemoMark is PlayerMark mark)
                    cell = new BoardCellPresentation(actualCell.Coordinate,
                        mark == PlayerMark.Paw ? BoardCellKind.Paw : mark == PlayerMark.X ? BoardCellKind.X : BoardCellKind.Empty,
                        actualCell.Direction, false);
                var tile = new VisualElement { name = "cell-" + cell.Coordinate.Row + "-" + cell.Coordinate.Column };
                tile.style.position = Position.Absolute;
                var fill = cell.Kind == BoardCellKind.Arrow || cell.Kind == BoardCellKind.Protected ? ImmovableTile : Tile;
                // Tint the fills only. Dark vector symbols, hatching and clue text
                // keep their contrast while the entire tile grid becomes quieter.
                tile.style.backgroundColor = Presentation.IsCompleted ? Color.Lerp(fill, CompletionTint, .35f) : fill;
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
            ApplyTutorialHighlights();
            if (arrowHold?.IsHighlighted == true) ApplyArrowHighlights();
        }
        public void ShowGenerationFailure(GenerationStatus generationStatus, string locale = "en", DifficultyProfile profile = null)
        {
            if (generationStatus == GenerationStatus.Success) throw new ArgumentException("A failure status is required.", nameof(generationStatus));
            CancelInteraction(); gesture = null; arrowHold = null;
            hintHighlight = null; ++highlightVersion;
            session = null; Presentation = null; board.Clear(); SetLocale(locale);
            difficulty.text = text.GetText("board." + (profile ?? DifficultyProfile.Easy).Id);
            board.style.display = DisplayStyle.None;
            error.style.display = DisplayStyle.Flex;
            error.text = new GenerationLocalizer(text).Format(generationStatus);
            ActivityChanged?.Invoke();
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
            var difficultyKey = session?.Puzzle.Size switch
            {
                6 => "board.medium", 7 => "board.hard", _ => "board.easy"
            };
            title.text = text.GetText("board.title"); difficulty.text = text.GetText(difficultyKey); intro.text = text.GetText("board.intro");
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
                swatch.style.backgroundColor = kind == BoardCellKind.Arrow || kind == BoardCellKind.Protected ? ImmovableTile : Tile;
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
                arrowHold = new ArrowHoldController(session, nextLayout, holdClock,
                    canBegin: coordinate => tutorial == null || tutorial.CanHoldArrow(coordinate),
                    activated: coordinate => tutorial?.RecordArrowHold(coordinate));
                gesture = new BoardGestureController(session, nextLayout,
                    canInteract: coordinate => tutorial == null ? session.CanInteract(coordinate) : tutorial.CanInteract(coordinate),
                    tap: coordinate => tutorial == null ? session.Tap(coordinate) : tutorial.Tap(coordinate),
                    applyDrag: coordinates => tutorial == null ? session.ApplyDrag(coordinates) : tutorial.ApplyDrag(coordinates));
            }
            Layout = nextLayout;
            Absolute(board, new Rect((boardRegion.contentRect.width - Layout.GridSize) / 2 - Layout.ClueGutter,
                (boardRegion.contentRect.height - Layout.GridSize) / 2 - Layout.ClueGutter, Layout.Width, Layout.Height));
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
            var pointerId = activityPointerId;
            var endedGesture = gesture?.HasActivePointer == true || arrowHold?.HasActivePointer == true;
            activityPointerId = -1;
            gesture?.Cancel();
            ClearArrowHold();
            if (pointerId < 0) return;
            ActivityChanged?.Invoke();
            if (board.HasPointerCapture(pointerId)) board.ReleasePointer(pointerId);
            if (endedGesture) InteractionEnded?.Invoke();
        }
        private Vector2 BoardPosition(Vector3 position) => board.WorldToLocal(new Vector2(position.x, position.y));
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!InteractionEnabled || !evt.isPrimary || evt.button != 0 || gesture == null ||
                HasActiveInteraction) return;
            var position = BoardPosition(evt.position);
            if (!gesture.TryGetCell(position, out _)) return;
            activityPointerId = evt.pointerId;
            ActivityChanged?.Invoke();
            if (arrowHold != null && arrowHold.Begin(evt.pointerId, position))
            {
                var controller = arrowHold;
                var pointerId = evt.pointerId;
                holdTick = schedule.Execute(() =>
                {
                    if (ReferenceEquals(controller, arrowHold) && controller.HasActivePointer && controller.ActivePointerId == pointerId)
                        TickArrowHold();
                }).Every(16);
            }
            else gesture.Begin(evt.pointerId, position);
            // Capture belongs to the stable board, never a tile replaced by Refresh.
            board.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
        private void TickArrowHold()
        {
            if (arrowHold?.HasActivePointer != true) return;
            var origin = arrowHold.Origin;
            var path = arrowHold.Path;
            var pointerId = arrowHold.ActivePointerId;
            if (arrowHold.Tick()) ApplyArrowHighlights();
            if (!arrowHold.HasActivePointer)
            {
                RestoreArrowHighlights(origin, path);
                holdTick?.Pause(); holdTick = null;
                EndActivity(pointerId);
                InteractionEnded?.Invoke();
            }
        }
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (arrowHold?.HasActivePointer == true && arrowHold.ActivePointerId == evt.pointerId)
            {
                var origin = arrowHold.Origin;
                var path = arrowHold.Path;
                arrowHold.Move(evt.pointerId, BoardPosition(evt.position));
                if (!arrowHold.HasActivePointer)
                {
                    RestoreArrowHighlights(origin, path);
                    holdTick?.Pause(); holdTick = null;
                    EndActivity(evt.pointerId);
                    InteractionEnded?.Invoke();
                }
                evt.StopPropagation(); return;
            }
            if (gesture == null || !gesture.HasActivePointer || gesture.ActivePointerId != evt.pointerId) return;
            var changed = gesture.Move(evt.pointerId, BoardPosition(evt.position));
            FinishPointerEvent(evt.pointerId, changed);
            if (!gesture.HasActivePointer && HasActiveInteraction) { EndActivity(evt.pointerId); InteractionEnded?.Invoke(); }
            evt.StopPropagation();
        }
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0) return;
            if (arrowHold?.HasActivePointer == true && arrowHold.ActivePointerId == evt.pointerId)
            {
                CancelInteraction(); evt.StopPropagation(); return;
            }
            if (activityPointerId != evt.pointerId) return;
            if (gesture == null || !gesture.HasActivePointer) { EndActivity(evt.pointerId); evt.StopPropagation(); return; }
            var changed = gesture.End(evt.pointerId, BoardPosition(evt.position));
            EndActivity(evt.pointerId);
            FinishPointerEvent(evt.pointerId, changed);
            InteractionEnded?.Invoke();
            evt.StopPropagation();
        }
        private void EndActivity(int pointerId)
        {
            if (activityPointerId != pointerId) return;
            activityPointerId = -1;
            ActivityChanged?.Invoke();
            if (board.HasPointerCapture(pointerId)) board.ReleasePointer(pointerId);
        }
        private bool OwnsPointer(int pointerId) => activityPointerId == pointerId;
        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (OwnsPointer(evt.pointerId)) CancelInteraction();
        }
        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (OwnsPointer(evt.pointerId)) CancelInteraction();
        }
        private void FinishPointerEvent(int pointerId, bool changed)
        {
            if (changed) { Refresh(); StateChanged?.Invoke(); }
        }
        private void ClearArrowHold()
        {
            holdTick?.Pause(); holdTick = null;
            if (arrowHold == null) return;
            var origin = arrowHold.Origin;
            var path = arrowHold.Path;
            arrowHold.Cancel();
            RestoreArrowHighlights(origin, path);
        }
        private void ApplyArrowHighlights()
        {
            RestoreCellOutline(arrowHold.Origin);
            foreach (var coordinate in arrowHold.Path) RestoreCellOutline(coordinate);
        }
        private void RestoreArrowHighlights(BoardCoordinate origin, IReadOnlyList<BoardCoordinate> path)
        {
            RestoreCellOutline(origin);
            foreach (var coordinate in path) RestoreCellOutline(coordinate);
        }
        private void RestoreCellOutline(BoardCoordinate coordinate)
        {
            var tile = board.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column);
            if (tile == null) return;
            var outlined = (hintHighlight.HasValue && hintHighlight.Value.Equals(coordinate)) ||
                (tutorial != null && System.Linq.Enumerable.Contains(tutorial.HighlightedCells, coordinate)) ||
                (arrowHold?.IsHighlighted == true && (arrowHold.Origin.Equals(coordinate) ||
                    System.Linq.Enumerable.Contains(arrowHold.Path, coordinate)));
            if (outlined) { Outline(tile, 3); return; }
            tile.style.borderLeftWidth = tile.style.borderRightWidth = tile.style.borderTopWidth = tile.style.borderBottomWidth = 1;
            tile.style.borderLeftColor = tile.style.borderRightColor = tile.style.borderTopColor = tile.style.borderBottomColor = Border;
            tile.style.borderTopLeftRadius = tile.style.borderTopRightRadius = tile.style.borderBottomLeftRadius = tile.style.borderBottomRightRadius = 7;
        }
        private void ApplyTutorialHighlights()
        {
            if (tutorial == null) return;
            foreach (var coordinate in tutorial.HighlightedCells)
                Outline(board.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column), 3);
            foreach (var row in tutorial.HighlightedRows) Outline(board.Q<Label>("row-clue-" + row), 2);
            foreach (var column in tutorial.HighlightedColumns) Outline(board.Q<Label>("column-clue-" + column), 2);
        }
        private static void Outline(VisualElement element, int width)
        {
            if (element == null) return;
            element.style.borderLeftWidth = element.style.borderRightWidth = element.style.borderTopWidth = element.style.borderBottomWidth = width;
            element.style.borderLeftColor = element.style.borderRightColor = element.style.borderTopColor = element.style.borderBottomColor = BoardSymbolElement.Ink;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 5;
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
