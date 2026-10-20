using System.Collections;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    // Issue #48: exercise the real shell eligibility against a mounted panel.
    public sealed class HintAttentionShellTests
    {
        private GameObject host;
        private UIDocument document;
        private PanelSettings panelSettings;
        private GameShellView view;
        private double now;

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (panelSettings != null) Object.DestroyImmediate(panelSettings);
        }

        private IEnumerator Mount(string difficulty = "easy", bool inline = true, EasyGameLoop loop = null)
        {
            now = 0;
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            host = new GameObject("issue48-headless-shell", typeof(UIDocument));
            host.hideFlags = HideFlags.HideAndDontSave;
            document = host.GetComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.rootVisualElement.style.width = 390;
            document.rootVisualElement.style.height = 844;
            view = new GameShellView(14, "en", loop ?? PlayTestFactory.Loop(difficultySelection: PlayTestFactory.Selection(difficulty)),
                useInlineHints: inline, tutorialSeen: () => true, markTutorialSeen: () => { }, idleClock: () => now);
            document.rootVisualElement.Add(view);
            Assert.That(view.StartNewGame(), Is.True);
            yield return null;
            yield return null;
            view.TickHintAttention();
            Assert.That(view.panel, Is.Not.Null);
        }
        private Button Hint => view.Q<Button>("action-hint");
        private void Advance(double seconds) { now += seconds; view.TickHintAttention(); }
        private static void AssertAttention(Button button, bool expected)
        {
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(expected ? BoardSymbolElement.Ink : PortraitBoardView.Tile));
            Assert.That(button.style.color.value, Is.EqualTo(expected ? PortraitBoardView.Tile : BoardSymbolElement.Ink));
            Assert.That(button.style.borderLeftWidth.value, Is.EqualTo(1));
            Assert.That(button.style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Normal));
            Assert.That(button.style.paddingLeft.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(button.style.transitionProperty.value.Select(property => property.ToString()),
                Is.EqualTo(new[] { "background-color", "color" }));
            Assert.That(button.style.transitionDuration.value?.FirstOrDefault().value ?? 0, Is.EqualTo(expected ? .2f : 0));
        }

        [UnityTest]
        public IEnumerator AllOrdinaryDifficultiesUseSameBoundaryWithoutMutatingMarks()
        {
            foreach (var difficulty in new[] { "easy", "medium", "hard" })
            {
                yield return Mount(difficulty);
                var before = view.ActiveSession.Marks.ToArray();
                var label = Hint.text;
                Advance(5.999); AssertAttention(Hint, false);
                Advance(.002); AssertAttention(Hint, true);
                Advance(30); AssertAttention(Hint, true);
                Assert.That(Hint.text, Is.EqualTo(label));
                Assert.That(view.ActiveSession.Marks, Is.EquivalentTo(before));
                TearDown(); host = null; panelSettings = null;
            }
        }

        [UnityTest]
        public IEnumerator HintAndClearResetWithoutChangingImmediateHintSemantics()
        {
            yield return Mount();
            Advance(6); AssertAttention(Hint, true);
            yield return null; // Let the fade start before exercising an immediate reset.
            var before = view.ActiveSession.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
            view.RequestHint(); AssertAttention(Hint, false);
            Assert.That(view.ActiveSession.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
            yield return null;
            yield return null;
            Assert.That(Hint.resolvedStyle.backgroundColor, Is.EqualTo(PortraitBoardView.Tile));
            Assert.That(Hint.resolvedStyle.color, Is.EqualTo(BoardSymbolElement.Ink));
            Advance(5.999); AssertAttention(Hint, false);
            Advance(.002); AssertAttention(Hint, true);
            view.ClearBoard(); AssertAttention(Hint, false);
            Assert.That(view.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Advance(6); AssertAttention(Hint, true);
        }

        [UnityTest]
        public IEnumerator HintWithUnavailableDeductionStillResets()
        {
            yield return Mount();
            // A deliberately ambiguous session exercises the defensive no-result path;
            // ordinary generated boards still pass their existing solver admission.
            var puzzle = new PuzzleDefinition(2, 1, "issue48-no-hint",
                new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 1) }, System.Array.Empty<ArrowClue>());
            var session = new GameSession(puzzle);
            typeof(EasyGameLoop).GetProperty(nameof(EasyGameLoop.Session)).SetValue(view.Loop, session);
            view.BoardView.Bind(session);
            Assert.That(session.ApplyHint().Status, Is.EqualTo(HintStatus.Unavailable));
            Advance(6); AssertAttention(Hint, true);
            var before = session.Marks.ToArray();
            view.RequestHint(); AssertAttention(Hint, false);
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Advance(5.999); AssertAttention(Hint, false);
            Advance(.002); AssertAttention(Hint, true);
        }

        [UnityTest]
        public IEnumerator NavigationModalFocusPauseAndSuspensionStartFreshIntervals()
        {
            yield return Mount(inline: false);
            Advance(6); AssertAttention(Hint, true);
            view.RequestHint(); Assert.That(view.Modal, Is.EqualTo("hint")); AssertAttention(Hint, false);
            Advance(60); AssertAttention(Hint, false);
            view.BackToGame(); Advance(5.999); AssertAttention(Hint, false);
            Advance(.002); AssertAttention(Hint, true);
            view.ShowMenu(); AssertAttention(Hint, false);
            Advance(60); view.BackToGame(); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
            view.SetApplicationFocus(false); AssertAttention(Hint, false);
            Advance(60); view.SetApplicationFocus(true); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
            view.SetApplicationPaused(true); AssertAttention(Hint, false);
            view.SetApplicationFocus(false); view.SetApplicationFocus(true);
            Advance(60); AssertAttention(Hint, false);
            view.SetApplicationPaused(false); Advance(5.999); AssertAttention(Hint, false);
            Advance(.002); AssertAttention(Hint, true);
            view.BoardView.SetInteractionSuspended(true); AssertAttention(Hint, false);
            Advance(60); view.BoardView.SetInteractionSuspended(false); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
            Hint.SetEnabled(false); view.TickHintAttention(); AssertAttention(Hint, false);
            Advance(60); Hint.SetEnabled(true); view.TickHintAttention(); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
        }

        [UnityTest]
        public IEnumerator RebindRebuildAndDetachCannotStyleOldButtons()
        {
            yield return Mount();
            Advance(6); AssertAttention(Hint, true);
            view.BoardView.Bind(view.ActiveSession); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
            var old = Hint;
            view.ShowMenu(); view.BackToGame();
            Assert.That(Hint, Is.Not.SameAs(old));
            AssertAttention(old, false); AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true); AssertAttention(old, false);
            view.RemoveFromHierarchy(); AssertAttention(Hint, false);
            Advance(60); AssertAttention(Hint, false);
            document.rootVisualElement.Add(view);
            yield return null;
            Advance(5.999); AssertAttention(Hint, false);
            Advance(.002); AssertAttention(Hint, true);
        }

        [UnityTest]
        public IEnumerator CompletionAndBothTutorialPhasesRemainIneligible()
        {
            yield return Mount();
            Advance(6); AssertAttention(Hint, true);
            for (var request = 0; request < 100 && !view.ActiveSession.IsCompleted; request++) view.RequestHint();
            Assert.That(view.ActiveSession.IsCompleted, Is.True);
            AssertAttention(Hint, false);
            Advance(60); AssertAttention(Hint, false);
            Assert.That(Hint.enabledSelf, Is.False);
            view.StartTutorial();
            Advance(60); Assert.That(Hint, Is.Null);
            var tutorial = view.Tutorial;
            for (var action = 0; action < 30 && tutorial.Step != TutorialStep.Independent; action++)
            {
                if (tutorial.CanContinue) { view.ContinueTutorial(); continue; }
                if (tutorial.Step == TutorialStep.DiagonalPaw)
                    Assert.That(tutorial.RecordArrowHold(new BoardCoordinate(0, 3)), Is.True);
                if (tutorial.Step == TutorialStep.Practice)
                    for (var tap = 0; tap < 3; tap++) tutorial.Tap(tutorial.Targets[0]);
                else foreach (var target in tutorial.Targets)
                {
                    var expected = tutorial.Session.Puzzle.IsSolutionPaw(target) ? PlayerMark.Paw : PlayerMark.X;
                    for (var tap = 0; tap < 3 && tutorial.Session.GetMark(target) != expected; tap++)
                        Assert.That(tutorial.Tap(target), Is.True);
                    Assert.That(tutorial.Session.GetMark(target), Is.EqualTo(expected));
                }
                Assert.That(tutorial.EndGesture(), Is.True);
            }
            view.BoardView.Bind(tutorial.Session); view.BackToGame();
            Assert.That(view.Tutorial.Step, Is.EqualTo(TutorialStep.Independent));
            Assert.That(view.ActiveSession.IsCompleted, Is.False);
            Advance(60); Assert.That(Hint, Is.Null);
            view.SkipTutorial(); Advance(60); Assert.That(Hint, Is.Null);
            Assert.That(view.Modal, Is.EqualTo("loading"));
            view.StartNewGame();
            AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
        }

        [UnityTest]
        public IEnumerator GenerationFailureAndRetryDoNotAccumulateIdleTime()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var requests = 0;
            var loop = PlayTestFactory.Loop(generate: seed => ++requests == 2 ? null : generator.Generate(DifficultyProfile.Easy, seed));
            yield return Mount(loop: loop);
            Advance(6); AssertAttention(Hint, true);
            Assert.That(view.StartNewGame(), Is.False);
            AssertAttention(Hint, false); Advance(60); AssertAttention(Hint, false);
            Assert.That(view.StartNewGame(), Is.True);
            AssertAttention(Hint, false);
            Advance(6); AssertAttention(Hint, true);
        }
    }
}
