using System;
using System.Collections.Generic;
using System.Diagnostics;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Sends gameplay intents and presents their results. No puzzle truth is evaluated here.
    public sealed class GameShellView : VisualElement
    {
        private readonly VisualElement safeFrame = new VisualElement();
        private readonly VisualElement menu = new VisualElement();
        private readonly VisualElement controls = new VisualElement();
        private readonly VisualElement overlay = new VisualElement();
        private readonly LocalizationService text;
        private Button hintButton;
        private readonly HintIdleAttention hintAttention;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private Button clearButton;
        private bool loading;
        private int queuedRequest;
        private readonly bool enableTutorial;
        private readonly Func<bool> tutorialSeen;
        private readonly Action markTutorialSeen;
        public TutorialController Tutorial { get; private set; }
        public GameSession ActiveSession => Tutorial?.Session ?? Loop.Session;
        private HintResult completionHint;
        // True selects immediate assistance; false retains the approved modal rollback.
        public bool UseInlineHints { get; }
        public PortraitBoardView BoardView { get; }
        public EasyGameLoop Loop { get; }
        public string Locale => text.SelectedLocale;
        public string Screen { get; private set; } = "menu";
        public string Modal { get; private set; }
        public long NewGameStartedAtTimestamp { get; private set; }
        public int NewGameRequestId { get; private set; }

        public GameShellView(int initialSeed = 14, string locale = null, EasyGameLoop loop = null, bool useInlineHints = true,
            bool enableTutorial = true, Func<bool> tutorialSeen = null, Action markTutorialSeen = null, IDifficultyHistoryStore difficultyHistory = null, Func<double> idleClock = null)
        {
            name = "game-shell";
            var clock = idleClock ?? (() => Time.realtimeSinceStartupAsDouble);
            hintAttention = new HintIdleAttention(clock);
            UseInlineHints = useInlineHints;
            this.enableTutorial = enableTutorial;
            if (tutorialSeen == null) FirstPlayPreference.Initialize();
            this.tutorialSeen = tutorialSeen ?? FirstPlayPreference.HasPlayed;
            this.markTutorialSeen = markTutorialSeen ?? FirstPlayPreference.MarkPlayed;
            Loop = loop ?? new EasyGameLoop(initialSeed, difficultySelection:
                DifficultySelection.CreateSeeded(initialSeed, difficultyHistory ?? new DifficultyHistoryPreference()));
            text = new LocalizationService(GameLocalization.CreateCatalog());
            text.SelectLanguage(locale ?? LanguagePreference.Load());
            style.flexGrow = 1; style.backgroundColor = PortraitBoardView.Background;
            style.color = BoardSymbolElement.Ink;
            style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BoardView = new PortraitBoardView(clock); BoardView.SetPlayLayout(); Add(BoardView);
            BoardView.StateChanged += OnBoardChanged;
            BoardView.ActivityChanged += OnBoardActivity;
            BoardView.InteractionEnded += OnTutorialGestureEnded;
            safeFrame.name = "game-safe-frame"; safeFrame.style.position = Position.Absolute;
            safeFrame.pickingMode = PickingMode.Ignore; Add(safeFrame);
            menu.name = "game-menu"; Fill(menu); safeFrame.Add(menu);
            controls.name = "game-controls"; controls.style.position = Position.Absolute;
            controls.style.left = controls.style.right = controls.style.bottom = 0; controls.style.height = 108;
            safeFrame.Add(controls);
            overlay.name = "game-overlay"; Fill(overlay); overlay.style.backgroundColor = PortraitBoardView.Background;
            safeFrame.Add(overlay);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateSafeArea());
            RegisterCallback<AttachToPanelEvent>(_ => { ResetHintAttention(); TickHintAttention(); });
            RegisterCallback<DetachFromPanelEvent>(_ => { hintAttention.Update(false, false); ApplyHintAttention(); });
            schedule.Execute(TickHintAttention).Every(50);
            ShowMenu();
        }

        public void SetApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            if (!focused) CancelInteraction();
            TickHintAttention();
        }
        public void SetApplicationPaused(bool paused)
        {
            applicationPaused = paused;
            if (paused) CancelInteraction();
            TickHintAttention();
        }
        // Manual ticking uses the same injected clock and lifecycle decisions as the scheduler.
        public void TickHintAttention()
        {
            var eligible = panel != null && applicationFocused && !applicationPaused && !loading &&
                Screen == "play" && Modal == null && Tutorial == null && ActiveSession != null &&
                !ActiveSession.IsCompleted && BoardView.IsBoundTo(ActiveSession) && BoardView.InteractionEnabled &&
                BoardView.style.display.value != DisplayStyle.None && controls.style.display.value != DisplayStyle.None &&
                hintButton != null && hintButton.enabledInHierarchy;
            hintAttention.Update(eligible, BoardView.HasActiveInteraction);
            ApplyHintAttention();
        }
        private void OnBoardActivity()
        {
            ResetHintAttention();
            TickHintAttention();
        }
        private void ResetHintAttention()
        {
            hintAttention.Reset();
            ApplyHintAttention();
        }
        private void ApplyHintAttention()
        {
            if (hintButton == null) return;
            var attention = hintAttention.IsAttentionVisible;
            if (attention == (hintButton.style.backgroundColor.value == BoardSymbolElement.Ink)) return;
            // Native color transitions fade in only; every reset cancels the fade immediately.
            hintButton.style.transitionDuration = new List<TimeValue> { new TimeValue(attention ? .2f : 0) };
            hintButton.style.backgroundColor = attention ? BoardSymbolElement.Ink : PortraitBoardView.Tile;
            hintButton.style.color = attention ? PortraitBoardView.Tile : BoardSymbolElement.Ink;
        }

        public void ApplySafeArea(Rect pixels, Vector2 screenPixels)
        {
            BoardView.ApplySafeArea(pixels, screenPixels);
            if (!float.IsFinite(contentRect.width) || !float.IsFinite(contentRect.height) || contentRect.width <= 0 || contentRect.height <= 0) return;
            var safe = SafeAreaLayout.ToPanelRect(pixels, screenPixels, new Vector2(contentRect.width, contentRect.height));
            safeFrame.style.left = safe.x + 16; safeFrame.style.top = safe.y + 16;
            safeFrame.style.width = Mathf.Max(0, safe.width - 32); safeFrame.style.height = Mathf.Max(0, safe.height - 32);
        }
        public void UpdateSafeArea() => ApplySafeArea(UnityEngine.Screen.safeArea, new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height));
        public void CancelInteraction() => BoardView.CancelInteraction();

        public void ShowMenu()
        {
            if (loading) return;
            BoardView.SetInteractionSuspended(true);
            BoardView.style.display = DisplayStyle.None;
            Screen = "menu"; Modal = null; overlay.style.display = DisplayStyle.None;
            controls.style.display = DisplayStyle.None; menu.style.display = DisplayStyle.Flex;
            TickHintAttention();
            menu.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1;
            scroll.contentContainer.style.paddingBottom = 8; menu.Add(scroll);
            scroll.Add(Heading(text.GetText("board.title"), 26));
            scroll.Add(Paragraph(text.GetText("board.intro")));
            scroll.Add(MakeButton("menu-start", "game.start", StartPlaying));
            if (ActiveSession != null) scroll.Add(MakeButton("menu-back", "game.back", BackToGame));
            if (enableTutorial) scroll.Add(MakeButton("menu-tutorial", "tutorial.title", StartTutorial));
            scroll.Add(MakeButton("menu-rules", "game.rules", ShowRules));
            scroll.Add(Paragraph(text.GetText("game.language")));
            AddLanguage(scroll, "en", "English");
            AddLanguage(scroll, "pt-BR", "Português (Brasil)");
            AddLanguage(scroll, "es", "Español");
        }
        private void AddLanguage(VisualElement parent, string locale, string label)
        {
            var button = new Button(() => SelectLanguage(locale)) { name = "language-" + locale, text = label };
            StyleButton(button); button.SetEnabled(locale != Locale); parent.Add(button);
        }
        public void SelectLanguage(string locale)
        {
            if (loading) return;
            CancelInteraction(); text.SelectLanguage(locale); LanguagePreference.Save(Locale);
            if (ActiveSession != null) BoardView.Bind(ActiveSession, Locale);
            ShowMenu();
        }
        public void StartPlaying()
        {
            if (loading || Screen != "menu" || Modal != null) return;
            if (enableTutorial && !tutorialSeen())
            {
                StartTutorial();
                markTutorialSeen();
            }
            else QueueNewGame();
        }
        public void StartTutorial()
        {
            if (loading || !enableTutorial) return;
            CancelInteraction(); completionHint = null;
            Tutorial = new TutorialController();
            BoardView.SetTutorial(Tutorial);
            BoardView.Bind(Tutorial.Session, Locale);
            BackToGame();
        }
        public void ContinueTutorial()
        {
            if (loading || Screen != "play" || Modal != null || Tutorial == null || !Tutorial.CanContinue) return;
            CancelInteraction();
            if (Tutorial.Continue()) RefreshTutorial();
        }
        public void SkipTutorial()
        {
            if (loading || Tutorial == null) return;
            QueueNewGame();
        }
        private void OnTutorialGestureEnded()
        {
            if (Tutorial == null) return;
            var previousSession = Tutorial.Session;
            if (!Tutorial.EndGesture()) return;
            if (!ReferenceEquals(previousSession, Tutorial.Session))
            {
                // Guided victory starts a fresh attempt at the same board. Rebind
                // the pointer recognizer as well as visuals to the new authority.
                BoardView.Bind(Tutorial.Session, Locale);
                BoardView.SetInteractionSuspended(Screen != "play" || Modal != null || Tutorial.Session.IsCompleted);
            }
            RefreshTutorial();
        }
        private void RefreshTutorial()
        {
            if (Tutorial == null) return;
            BoardView.Refresh();
            BoardView.SetTutorialFeedback(text.GetText(Tutorial.MessageKey), expanded: Tutorial.Step == TutorialStep.Goal);
            if (Screen == "play") BuildControls();
        }
        private void LeaveTutorial()
        {
            CancelInteraction();
            Tutorial = null; BoardView.SetTutorial(null); BoardView.SetTutorialFeedback(null);
        }
        public bool StartNewGame()
        {
            // A direct diagnostic/test request can consume a queued command. Its
            // scheduled callback must then become stale rather than start twice.
            ResetHintAttention();
            queuedRequest++;
            LeaveTutorial();
            completionHint = null; BoardView.SetCompletionFeedback(null);
            if (!loading) NewGameStartedAtTimestamp = Stopwatch.GetTimestamp();
            NewGameRequestId++;
            var success = Loop.NewGame(); loading = false;
            if (!success)
            {
                Screen = "failure"; menu.style.display = controls.style.display = DisplayStyle.None;
                BoardView.style.display = DisplayStyle.Flex;
                BoardView.ShowGenerationFailure(Loop.LastGenerationStatus ?? GenerationStatus.InvalidProfile, Locale, Loop.RequestedDifficulty);
                ShowModal("failure", text.GetText("game.failure"),
                    new GenerationLocalizer(text).Format(Loop.LastGenerationStatus ?? GenerationStatus.InvalidProfile),
                    MakeButton("modal-retry", "game.retry", QueueNewGame), MakeButton("modal-menu", "game.menu", ShowMenu));
                return false;
            }
            BoardView.Bind(ActiveSession, Locale); BackToGame();
            return true;
        }
        private void QueueNewGame()
        {
            if (loading) return;
            NewGameStartedAtTimestamp = Stopwatch.GetTimestamp();
            LeaveTutorial(); loading = true; BoardView.SetInteractionSuspended(true);
            ShowModal("loading", text.GetText("game.loading"), "");
            // Give feedback a frame before synchronous generation. One queued
            // command owns one start, even if a duplicate button event arrives.
            var request = ++queuedRequest;
            schedule.Execute(() => { if (loading && request == queuedRequest) StartNewGame(); }).StartingIn(1);
        }
        public void BackToGame()
        {
            if (loading || ActiveSession == null) return;
            Screen = "play"; Modal = null; menu.style.display = overlay.style.display = DisplayStyle.None;
            BoardView.style.display = controls.style.display = DisplayStyle.Flex;
            BoardView.SetInteractionSuspended(ActiveSession.IsCompleted);
            if (Tutorial != null) RefreshTutorial(); else { BuildControls(); UpdateCompletionFeedback(); }
        }
        private void BuildControls()
        {
            ResetHintAttention();
            controls.Clear();
            if (Tutorial != null) { BuildTutorialControls(); return; }
            var first = ActionRow(); var second = ActionRow();
            hintButton = MakeButton("action-hint", "game.hint", RequestHint);
            hintButton.style.transitionProperty = new List<StylePropertyName> { "background-color", "color" };
            clearButton = MakeButton("action-clear", "game.clear", ClearBoard);
            hintButton.SetEnabled(!ActiveSession.IsCompleted); clearButton.SetEnabled(!ActiveSession.IsCompleted);
            first.Add(hintButton); first.Add(clearButton);
            second.Add(MakeButton("action-menu", "game.menu", ShowMenu));
            second.Add(MakeButton("action-new-game", "game.new", QueueNewGame));
            foreach (var row in new[] { first, second })
                foreach (var child in row.Children()) { child.style.flexGrow = 1; child.style.flexBasis = 0; }
            controls.Add(first); controls.Add(second);
            TickHintAttention();
        }
        private void BuildTutorialControls()
        {
            hintButton = clearButton = null;
            var first = ActionRow(); var second = ActionRow();
            if (Tutorial.IsComplete) first.Add(MakeButton("action-play", "tutorial.play", SkipTutorial));
            else if (Tutorial.CanContinue)
            {
                var advance = MakeButton("action-continue", Tutorial.Step == TutorialStep.Begin ? "tutorial.start" : "tutorial.continue", ContinueTutorial);
                if (Tutorial.Step == TutorialStep.Welcome)
                {
                    advance.style.borderLeftWidth = advance.style.borderRightWidth = advance.style.borderTopWidth = advance.style.borderBottomWidth = 3;
                    advance.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                first.Add(advance);
            }
            second.Add(MakeButton("action-menu", "game.menu", ShowMenu));
            if (!Tutorial.IsComplete) second.Add(MakeButton("action-skip", "tutorial.skip", SkipTutorial));
            foreach (var row in new[] { first, second })
                foreach (var child in row.Children()) { child.style.flexGrow = 1; child.style.flexBasis = 0; }
            controls.Add(first); controls.Add(second);
        }
        public void ClearBoard()
        {
            if (Tutorial != null || loading || Screen != "play" || Modal != null || ActiveSession == null || ActiveSession.IsCompleted) return;
            ResetHintAttention(); CancelInteraction(); ActiveSession.Clear(); BoardView.Refresh();
            TickHintAttention();
        }
        public void RequestHint()
        {
            if (Tutorial != null || loading || Screen != "play" || Modal != null || ActiveSession == null || ActiveSession.IsCompleted) return;
            ResetHintAttention(); CancelInteraction(); var result = ActiveSession.ApplyHint(); BoardView.Refresh();
            TickHintAttention();
            if (result.Assignment.HasValue) BoardView.HighlightCell(result.Assignment.Value.Coordinate, UseInlineHints ? 700 : 0);
            if (UseInlineHints)
            {
                if (ActiveSession.IsCompleted) UpdateCompletionFeedback();
                return;
            }
            if (ActiveSession.IsCompleted)
            {
                completionHint = result;
                UpdateCompletionFeedback();
            }
            else ShowModal("hint", text.GetText("game.hint"), FormatHint(result), MakeButton("modal-back", "game.close", BackToGame));
        }
        private string FormatHint(HintResult result)
        {
            var explanation = text.GetText("game.hint-none");
            if (result.Step != null && result.Assignment.HasValue)
            {
                var assignment = result.Assignment.Value;
                explanation = new DeductionLocalizer(text).Format(result.Step) + "\n\n" + text.Format("game.hint-target",
                    new Dictionary<string, object> { { "row", assignment.Coordinate.Row + 1 }, { "column", assignment.Coordinate.Column + 1 },
                    { "state", text.GetText(assignment.State == CellKnowledge.Cat ? "board.cat" : "board.x") } });
                if (result.Corrected) explanation += "\n\n" + text.GetText("game.hint-correction");
            }
            return explanation;
        }
        private void OnBoardChanged()
        {
            TickHintAttention();
            if (Tutorial == null && ActiveSession?.IsCompleted == true) UpdateCompletionFeedback();
        }
        private void UpdateCompletionFeedback()
        {
            if (ActiveSession?.IsCompleted != true) { BoardView.SetCompletionFeedback(null); return; }
            hintButton?.SetEnabled(false); clearButton?.SetEnabled(false);
            TickHintAttention();
            BoardView.SetInteractionSuspended(true);
            BoardView.SetCompletionFeedback(text.GetText("game.victory"), text.GetText("game.last-hint"),
                UseInlineHints || completionHint == null ? null : (Action)ShowLastHint);
        }
        public void ShowLastHint()
        {
            if (UseInlineHints || loading || Screen != "play" || Modal != null || ActiveSession?.IsCompleted != true || completionHint == null) return;
            ShowModal("last-hint", text.GetText("game.last-hint"), FormatHint(completionHint),
                MakeButton("modal-back", "game.close", BackToGame));
        }
        public void ShowRules()
        {
            if (loading) return;
            var returnToMenu = Screen == "menu";
            ShowModal("rules", text.GetText("game.rules"), text.GetText("game.rules-body"),
                MakeButton("modal-back", "game.close", () => { if (returnToMenu) ShowMenu(); else BackToGame(); }));
        }
        private void ShowModal(string kind, string heading, string body, params Button[] actions)
        {
            BoardView.SetInteractionSuspended(true); Modal = kind; overlay.Clear(); overlay.style.display = DisplayStyle.Flex;
            TickHintAttention();
            overlay.style.paddingLeft = overlay.style.paddingRight = 8;
            overlay.style.paddingTop = overlay.style.paddingBottom = 8;
            overlay.Add(Heading(heading, 23));
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1; scroll.style.flexShrink = 1;
            var explanation = Paragraph(body);
            if (kind == "last-hint") explanation.name = "last-hint-explanation";
            scroll.Add(explanation); overlay.Add(scroll);
            var footer = new VisualElement(); footer.style.flexShrink = 0;
            foreach (var action in actions) footer.Add(action);
            overlay.Add(footer);
        }
        private Button MakeButton(string name, string key, Action action)
        {
            var button = new Button(action) { name = name, text = text.GetText(key) }; StyleButton(button); return button;
        }
        private static void StyleButton(Button button)
        {
            button.style.minHeight = 48; button.style.flexShrink = 0; button.style.fontSize = 16;
            button.style.whiteSpace = WhiteSpace.Normal; button.style.backgroundColor = PortraitBoardView.Tile;
            button.style.color = BoardSymbolElement.Ink; button.style.marginTop = button.style.marginBottom = 3;
            button.style.marginLeft = button.style.marginRight = 3;
            button.style.borderLeftColor = button.style.borderRightColor = button.style.borderTopColor = button.style.borderBottomColor = BoardSymbolElement.Ink;
            button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = 1;
        }
        private static VisualElement ActionRow()
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.height = 54;
            return row;
        }
        private static Label Heading(string value, int fontSize)
        {
            var label = Paragraph(value); label.style.fontSize = fontSize; label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter; return label;
        }
        private static Label Paragraph(string value)
        {
            var label = new Label(value); label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = 16;
            label.style.marginTop = label.style.marginBottom = 8; label.style.flexShrink = 0; return label;
        }
        private static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute; element.style.left = element.style.right = element.style.top = element.style.bottom = 0;
        }
    }
}
