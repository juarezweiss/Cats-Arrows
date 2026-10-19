using System;
using System.Collections.Generic;
using System.Diagnostics;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
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
        private readonly VisualElement completionLayer = new VisualElement();
        private readonly Label completionMessage = new Label();
        private readonly VisualElement completionActions = new VisualElement();
        private readonly CompletionConfettiElement completionConfetti = new CompletionConfettiElement();
        private readonly CompletionCelebration celebration;
        private GameSession celebratingSession;
        private readonly LocalizationService text;
        private Button hintButton;
        private readonly HintIdleAttention hintAttention;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private Button clearButton;
        private bool loading;
        private bool nextBoardPending;
        private int queuedRequest;
        private readonly MonetizationCoordinator monetization;
        private bool CommercialBusy => monetization?.IsBusy == true;
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
            bool enableTutorial = true, Func<bool> tutorialSeen = null, Action markTutorialSeen = null, IDifficultyHistoryStore difficultyHistory = null, Func<double> idleClock = null, MonetizationCoordinator monetization = null, Func<double> completionClock = null)
        {
            name = "game-shell";
            this.monetization = monetization;
            var clock = idleClock ?? (() => Time.realtimeSinceStartupAsDouble);
            hintAttention = new HintIdleAttention(clock);
            celebration = new CompletionCelebration(completionClock ?? (() => Time.realtimeSinceStartupAsDouble));
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
            BoardView.Rebound += CancelCompletion;
            safeFrame.name = "game-safe-frame"; safeFrame.style.position = Position.Absolute;
            safeFrame.pickingMode = PickingMode.Ignore; Add(safeFrame);
            menu.name = "game-menu"; Fill(menu); safeFrame.Add(menu);
            BuildCompletionLayer();
            safeFrame.Add(completionLayer);
            controls.name = "game-controls"; controls.style.position = Position.Absolute;
            controls.style.left = controls.style.right = controls.style.bottom = 0; controls.style.height = 108;
            safeFrame.Add(controls);
            overlay.name = "game-overlay"; Fill(overlay); overlay.style.backgroundColor = PortraitBoardView.Background;
            safeFrame.Add(overlay);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateSafeArea());
            if (monetization != null)
            {
                monetization.Changed += OnMonetizationChanged;
                monetization.HintDelivered += OnHintDelivered;
            }
            RegisterCallback<AttachToPanelEvent>(_ => { ResetHintAttention(); TickHintAttention(); });
            RegisterCallback<DetachFromPanelEvent>(_ => { hintAttention.Update(false, false); ApplyHintAttention(); CancelCompletion(); });
            schedule.Execute(TickHintAttention).Every(50);
            // One view-owned tick; no per-win callbacks survive navigation or rebind.
            schedule.Execute(TickCompletion).Every(16);
            ShowMenu();
        }

        public void SetApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            if (!focused) { CancelInteraction(); CancelCompletion(); }
            TickHintAttention();
        }
        public void SetApplicationPaused(bool paused)
        {
            applicationPaused = paused;
            if (paused) { CancelInteraction(); CancelCompletion(); }
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

        private void BuildCompletionLayer()
        {
            completionLayer.name = "completion-layer";
            completionLayer.pickingMode = PickingMode.Ignore;
            completionLayer.style.position = Position.Absolute;
            completionLayer.style.left = completionLayer.style.right = 0;
            completionLayer.style.top = 108; completionLayer.style.bottom = 120;
            completionLayer.style.alignItems = Align.Center;
            completionLayer.style.justifyContent = Justify.Center;
            completionLayer.style.display = DisplayStyle.None;
            completionLayer.Add(completionConfetti);
            completionMessage.name = "completion-message";
            completionMessage.pickingMode = PickingMode.Ignore;
            completionMessage.style.fontSize = 22;
            completionMessage.style.whiteSpace = WhiteSpace.Normal;
            completionMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
            completionMessage.style.backgroundColor = PortraitBoardView.Background;
            completionMessage.style.paddingLeft = completionMessage.style.paddingRight = 12;
            completionMessage.style.paddingTop = completionMessage.style.paddingBottom = 12;
            completionMessage.style.width = Length.Percent(100); completionMessage.style.maxWidth = 340;
            completionLayer.Add(completionMessage);
            completionActions.name = "completion-actions";
            completionActions.style.width = Length.Percent(100); completionActions.style.maxWidth = 300;
            completionActions.Add(MakeButton("completion-next", "completion.next", () =>
            { if (CanUseCompletionActions()) QueueNewGame(); }));
            completionActions.Add(MakeButton("completion-back", "completion.back", BackToCompletedBoard));
            completionActions.Add(MakeButton("completion-menu", "completion.menu", () =>
            { if (CanUseCompletionActions()) ShowMenu(); }));
            completionLayer.Add(completionActions);
        }

        public void TickCompletion()
        {
            if (!celebration.IsVisible) return;
            if (!CanPresentCompletion()) { CancelCompletion(); return; }
            celebration.Tick();
            RenderCompletion();
        }
        private bool CanPresentCompletion() => panel != null && applicationFocused && !applicationPaused && !loading &&
            !CommercialBusy && Screen == "play" && Modal == null && Tutorial == null &&
            ActiveSession?.IsCompleted == true && ReferenceEquals(celebratingSession, ActiveSession) && BoardView.IsBoundTo(ActiveSession);
        private bool CanUseCompletionActions() => celebration.ActionsEnabled && CanPresentCompletion();
        public void BackToCompletedBoard()
        {
            if (!CanUseCompletionActions()) return;
            CancelCompletion();
        }
        private void CancelCompletion()
        {
            celebration.Cancel();
            RenderCompletion();
        }
        private void RenderCompletion()
        {
            var visible = celebration.IsVisible;
            completionLayer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            BoardView.SetCompletionOpacity(celebration.BoardOpacity);
            completionMessage.style.display = visible && (int)celebration.Phase <= (int)CompletionPhase.MessageFadeOut ? DisplayStyle.Flex : DisplayStyle.None;
            completionMessage.style.opacity = celebration.MessageOpacity;
            completionActions.style.display = visible && (int)celebration.Phase >= (int)CompletionPhase.ActionsFadeIn ? DisplayStyle.Flex : DisplayStyle.None;
            completionActions.style.opacity = celebration.ActionsOpacity;
            completionActions.SetEnabled(celebration.ActionsEnabled && CanPresentCompletion());
            completionConfetti.SetElapsed(celebration.HasConfetti ? celebration.Elapsed : -1);
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
            if (loading || CommercialBusy) return;
            CancelCompletion();
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
            AddMonetizationMenu(scroll);
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
            if (loading || CommercialBusy) return;
            CancelInteraction(); text.SelectLanguage(locale); LanguagePreference.Save(Locale);
            if (ActiveSession != null) BoardView.Bind(ActiveSession, Locale);
            ShowMenu();
        }
        public void StartPlaying()
        {
            if (loading || CommercialBusy || Screen != "menu" || Modal != null) return;
            if (enableTutorial && !tutorialSeen())
            {
                StartTutorial();
                markTutorialSeen();
            }
            else QueueNewGame();
        }
        public void StartTutorial()
        {
            if (loading || CommercialBusy || !enableTutorial) return;
            CancelCompletion();
            CancelInteraction(); completionHint = null;
            Tutorial = new TutorialController();
            monetization?.BindSession(Tutorial.Session, ordinary: false);
            BoardView.SetTutorial(Tutorial);
            BoardView.Bind(Tutorial.Session, Locale);
            BackToGame();
        }
        public void ContinueTutorial()
        {
            if (loading || CommercialBusy || Screen != "play" || Modal != null || Tutorial == null || !Tutorial.CanContinue) return;
            CancelInteraction();
            if (Tutorial.Continue()) RefreshTutorial();
        }
        public void SkipTutorial()
        {
            if (loading || CommercialBusy || Tutorial == null) return;
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
                monetization?.BindSession(Tutorial.Session, ordinary: false);
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
            if (nextBoardPending || CommercialBusy) return false;
            CancelCompletion();
            // Consume any queued command before an asynchronous next-board transition.
            ResetHintAttention();
            queuedRequest++;
            if (!loading) NewGameStartedAtTimestamp = Stopwatch.GetTimestamp();
            monetization?.RecordCompletion();
            if (monetization?.Enabled != true) return GenerateNewGame();
            nextBoardPending = true; loading = true;
            CancelInteraction();
            ShowModal("loading", text.GetText("game.loading"), "");
            var result = false;
            monetization.BeginNextBoard(() =>
            {
                nextBoardPending = false;
                result = GenerateNewGame();
            });
            // False also means an asynchronous transition is pending; no board was generated yet.
            return result;
        }
        private bool GenerateNewGame()
        {
            LeaveTutorial();
            completionHint = null; BoardView.SetCompletionFeedback(null);
            if (!loading) NewGameStartedAtTimestamp = Stopwatch.GetTimestamp();
            NewGameRequestId++;
            var success = Loop.NewGame(); loading = false;
            if (!success)
            {
                monetization?.BindSession(null);
                Screen = "failure"; menu.style.display = controls.style.display = DisplayStyle.None;
                BoardView.style.display = DisplayStyle.Flex;
                BoardView.ShowGenerationFailure(Loop.LastGenerationStatus ?? GenerationStatus.InvalidProfile, Locale, Loop.RequestedDifficulty);
                ShowModal("failure", text.GetText("game.failure"),
                    new GenerationLocalizer(text).Format(Loop.LastGenerationStatus ?? GenerationStatus.InvalidProfile),
                    MakeButton("modal-retry", "game.retry", QueueNewGame), MakeButton("modal-menu", "game.menu", ShowMenu));
                return false;
            }
            monetization?.BindSession(ActiveSession);
            BoardView.Bind(ActiveSession, Locale); BackToGame();
            return true;
        }
        private void QueueNewGame()
        {
            if (loading || CommercialBusy) return;
            CancelCompletion();
            NewGameStartedAtTimestamp = Stopwatch.GetTimestamp();
            if (Tutorial != null) LeaveTutorial();
            loading = true; BoardView.SetInteractionSuspended(true);
            ShowModal("loading", text.GetText("game.loading"), "");
            // Give feedback a frame before synchronous generation. One queued
            // command owns one start, even if a duplicate button event arrives.
            var request = ++queuedRequest;
            schedule.Execute(() => { if (loading && request == queuedRequest) StartNewGame(); }).StartingIn(1);
        }
        public void BackToGame()
        {
            if (loading || CommercialBusy || ActiveSession == null) return;
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
            hintButton = MakeButton("action-hint", monetization?.Enabled == true && !monetization.IsOwned ? "monetization.hint-ads" : "game.hint", RequestHint);
            hintButton.style.transitionProperty = new List<StylePropertyName> { "background-color", "color" };
            clearButton = MakeButton("action-clear", "game.clear", ClearBoard);
            hintButton.SetEnabled(!ActiveSession.IsCompleted && !CommercialBusy); clearButton.SetEnabled(!ActiveSession.IsCompleted && !CommercialBusy);
            first.Add(hintButton); first.Add(clearButton);
            second.Add(MakeButton("action-menu", "game.menu", ShowMenu));
            second.Add(MakeButton("action-new-game", "game.new", QueueNewGame));
            foreach (var row in new[] { first, second })
                foreach (var child in row.Children()) { child.style.flexGrow = 1; child.style.flexBasis = 0; }
            controls.Add(first); controls.Add(second);
            controls.SetEnabled(!CommercialBusy);
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
            if (Tutorial != null || loading || CommercialBusy || Screen != "play" || Modal != null || ActiveSession == null || ActiveSession.IsCompleted) return;
            ResetHintAttention(); CancelInteraction(); ActiveSession.Clear(); BoardView.Refresh();
            TickHintAttention();
        }
        public void RequestHint()
        {
            if (Tutorial != null || loading || CommercialBusy || Screen != "play" || Modal != null || ActiveSession == null || ActiveSession.IsCompleted) return;
            ResetHintAttention(); CancelInteraction();
            if (monetization?.Enabled == true) { monetization.RequestHint(); return; }
            PresentHint(ActiveSession.ApplyHint());
        }
        private void OnHintDelivered(HintDelivery delivery)
        {
            if (!ReferenceEquals(ActiveSession, delivery.Session) || Tutorial != null) return;
            PresentHint(delivery.Result);
        }
        private void PresentHint(HintResult result)
        {
            monetization?.RecordCompletion();
            BoardView.Refresh();
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
                    { "state", text.GetText(assignment.State == CellKnowledge.Paw ? "board.paw" : "board.x") } });
                if (result.Corrected) explanation += "\n\n" + text.GetText("game.hint-correction");
            }
            return explanation;
        }
        private void OnBoardChanged()
        {
            TickHintAttention();
            if (Tutorial == null && ActiveSession?.IsCompleted == true)
            {
                monetization?.RecordCompletion();
                UpdateCompletionFeedback();
            }
        }
        private void UpdateCompletionFeedback()
        {
            if (ActiveSession?.IsCompleted != true) { BoardView.SetCompletionFeedback(MonetizationMessage()); return; }
            hintButton?.SetEnabled(false); clearButton?.SetEnabled(false);
            TickHintAttention();
            BoardView.SetInteractionSuspended(true);
            BoardView.SetCompletionFeedback(text.GetText("game.victory"), text.GetText("game.last-hint"),
                UseInlineHints || completionHint == null ? null : (Action)ShowLastHint);
            if (Tutorial == null)
            {
                celebratingSession = ActiveSession;
                if (celebration.TryStart(ActiveSession))
                {
                    completionMessage.text = text.GetText("completion.message");
                    completionActions.Q<Button>("completion-next").text = text.GetText("completion.next");
                    completionActions.Q<Button>("completion-back").text = text.GetText("completion.back");
                    completionActions.Q<Button>("completion-menu").text = text.GetText("completion.menu");
                }
                if (!CanPresentCompletion()) celebration.Cancel();
                RenderCompletion();
            }
        }
        public void ShowLastHint()
        {
            if (UseInlineHints || loading || CommercialBusy || Screen != "play" || Modal != null || ActiveSession?.IsCompleted != true || completionHint == null) return;
            ShowModal("last-hint", text.GetText("game.last-hint"), FormatHint(completionHint),
                MakeButton("modal-back", "game.close", BackToGame));
        }
        private string MonetizationMessage()
        {
            if (monetization?.Enabled != true || monetization.Status == MonetizationStatus.None) return null;
            return text.GetText("monetization.status." + monetization.Status);
        }
        private void AddMonetizationMenu(VisualElement parent)
        {
            if (monetization?.Enabled != true) return;
            parent.Add(Paragraph(text.GetText("monetization.explanation")));
            if (monetization.Offer?.IsReviewOffer == true)
                parent.Add(Paragraph(text.GetText("monetization.review")));
            var purchase = MakeButton("menu-remove-ads", "monetization.remove", PurchaseRemoveAds);
            if (monetization.IsOwned) purchase.text = text.GetText("monetization.status.Owned");
            else if (monetization.Offer?.Available == true && !string.IsNullOrEmpty(monetization.Offer.LocalizedPrice))
                purchase.text += " · " + monetization.Offer.LocalizedPrice;
            purchase.SetEnabled(monetization.CanPurchase && !CommercialBusy); parent.Add(purchase);
            var restore = MakeButton("menu-restore", "monetization.restore", RestorePurchases);
            restore.SetEnabled(!CommercialBusy); parent.Add(restore);
            var status = Paragraph(MonetizationMessage()); status.name = "monetization-menu-status"; parent.Add(status);
        }
        public void PurchaseRemoveAds()
        {
            if (loading || CommercialBusy || Screen != "menu" || Modal != null) return;
            CancelInteraction(); monetization?.PurchaseRemoveAds();
        }
        public void RestorePurchases()
        {
            if (loading || CommercialBusy || Screen != "menu" || Modal != null) return;
            CancelInteraction(); monetization?.RestorePurchases();
        }
        private void OnMonetizationChanged()
        {
            if (loading) return;
            if (Screen == "menu" && Modal == null)
            {
                // Rebuild without allowing a busy menu action to escape input suspension.
                if (!CommercialBusy) ShowMenu();
                else
                {
                    menu.SetEnabled(false);
                    var status = menu.Q<Label>("monetization-menu-status");
                    if (status != null) status.text = MonetizationMessage();
                }
                if (!CommercialBusy) menu.SetEnabled(true);
                return;
            }
            if (Screen != "play") return;
            BuildControls();
            BoardView.SetInteractionSuspended(CommercialBusy || Modal != null || ActiveSession?.IsCompleted == true);
            if (Tutorial == null) UpdateCompletionFeedback();
        }
        public void SuspendMonetizationDelivery()
        {
            CancelInteraction(); monetization?.CancelHintDelivery();
        }
        public void RefreshMonetization() => monetization?.RefreshPurchases();
        public void TickMonetization() => monetization?.Tick();
        public void DisposeMonetization()
        {
            if (monetization == null) return;
            monetization.Changed -= OnMonetizationChanged;
            monetization.HintDelivered -= OnHintDelivered;
            monetization.Dispose();
        }
        public void ShowRules()
        {
            if (loading || CommercialBusy) return;
            var returnToMenu = Screen == "menu";
            ShowModal("rules", text.GetText("game.rules"), text.GetText(monetization?.Enabled == true ? "monetization.rules-body" : "game.rules-body"),
                MakeButton("modal-back", "game.close", () => { if (returnToMenu) ShowMenu(); else BackToGame(); }));
        }
        private void ShowModal(string kind, string heading, string body, params Button[] actions)
        {
            CancelCompletion();
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
