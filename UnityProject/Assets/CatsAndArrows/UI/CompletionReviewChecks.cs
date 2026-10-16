using System;
using System.Collections;
using System.IO;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #44: opt-in native-panel checks and captures; isolated from owner preferences.
    internal static class CompletionReviewChecks
    {
        public static IEnumerator Run(GameShellView shell, Action<double> advance, string captures)
        {
            for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Require(Path.IsPathRooted(captures), "absolute captures path"); Directory.CreateDirectory(captures);
                Require(shell.StartNewGame(), "ordinary board starts");
            })) yield break;
            for (var i = 0; i < 6; i++) yield return new WaitForEndOfFrame();
            foreach (var touch in new[] { false, true })
            {
                var session = shell.ActiveSession;
                var grid = shell.BoardView.Q("board-grid");
                var bounds = grid.worldBound;
                if (!Check(() =>
                {
                    if (touch)
                    {
                        foreach (var cat in session.Puzzle.SolutionCats)
                        { Tap(shell, cat, true); Tap(shell, cat, true); }
                    }
                    else for (var hint = 0; hint < 100 && !session.IsCompleted; hint++) Click(shell, "action-hint");
                    Require(session.IsCompleted && !shell.BoardView.InteractionEnabled, "tap/final-Hint domain lock");
                    Require(!shell.Q<Button>("action-hint").enabledSelf && !shell.Q<Button>("action-clear").enabledSelf, "completed controls locked");
                    advance(.8); shell.TickCompletion();
                })) yield break;
                for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    Require(shell.Q<Label>("completion-message").resolvedStyle.opacity == 1, "message fully visible");
                    Require(shell.Q("completion-actions").style.display.value == DisplayStyle.None, "no early actions");
                    Require(shell.Q("completion-confetti").style.display.value != DisplayStyle.None, "bilateral confetti visible");
                    Require(Delta(grid.worldBound, bounds) < .01f, "celebration does not move grid");
                    if (!touch) ScreenCapture.CaptureScreenshot(Path.Combine(captures, "celebration.png"));
                })) yield break;
                yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    advance(2.2); shell.TickCompletion();
                    Require(shell.Q("completion-confetti").style.display.value == DisplayStyle.None, "confetti gone at three seconds");
                    advance(2); shell.TickCompletion();
                })) yield break;
                for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    Require(shell.Q("completion-message").style.display.value == DisplayStyle.None, "message gone before actions");
                    var area = shell.Q("game-safe-frame").worldBound;
                    var controls = shell.Q("game-controls").worldBound;
                    foreach (var name in new[] { "completion-next", "completion-back", "completion-menu" })
                    {
                        var button = shell.Q<Button>(name); var rect = button.worldBound;
                        // World bounds snap to physical pixels at the panel scale.
                        Require(button.enabledInHierarchy && button.resolvedStyle.minHeight.value >= 48 && rect.height >= 47,
                            "48-unit target " + name + ": " + rect + " enabled=" + button.enabledInHierarchy);
                        Require(rect.xMin >= area.xMin - .01f && rect.xMax <= area.xMax + .01f && rect.yMin >= area.yMin && rect.yMax <= controls.yMin,
                            "safe centered action " + name + ": " + rect + " safe=" + area + " controls=" + controls);
                        Require(button.text == button.text.ToLowerInvariant(), "localized lowercase action " + name);
                    }
                    if (!touch) ScreenCapture.CaptureScreenshot(Path.Combine(captures, "actions.png"));
                })) yield break;
                yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    var marks = session.Marks.ToArray();
                    if (!touch)
                    {
                        Click(shell, "completion-back");
                        Require(shell.Q("completion-actions").style.display.value == DisplayStyle.None, "Back dismisses actions");
                        Require(grid.style.opacity.value == 1 && !shell.BoardView.InteractionEnabled, "Back restores static locked board");
                        Tap(shell, session.Marks.Keys.First(), false); shell.RequestHint(); shell.ClearBoard();
                        Require(session.Marks.SequenceEqual(marks), "finished board remains identical");
                        Click(shell, "action-menu");
                    }
                })) yield break;
                for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
                if (!touch && !Check(() => Click(shell, "menu-back"))) yield break;
                for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    if (!touch)
                    {
                        advance(50); shell.TickCompletion();
                        Require(shell.Q("completion-message").style.display.value == DisplayStyle.None, "Menu return never replays");
                        ScreenCapture.CaptureScreenshot(Path.Combine(captures, "static.png"));
                    }
                })) yield break;
                yield return new WaitForEndOfFrame();
                if (!Check(() => Click(shell, touch ? "completion-next" : "action-new-game"))) yield break;
                for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    Require(!ReferenceEquals(shell.ActiveSession, session) && !shell.ActiveSession.IsCompleted, "Next replaces board using ordinary selection");
                    Require(shell.Loop.DifficultySelection.History.ConsecutiveStarts <= 2, "persistent anti-streak selector");
                    advance(50); shell.TickCompletion();
                    Require(shell.Q("completion-actions").style.display.value == DisplayStyle.None, "stale tick cannot affect new board");
                })) yield break;
            }
            if (!Check(() =>
            {
                for (var hint = 0; hint < 100 && !shell.ActiveSession.IsCompleted; hint++) Click(shell, "action-hint");
                var session = shell.ActiveSession; Require(session.IsCompleted, "third completion");
                advance(5); shell.TickCompletion();
            })) yield break;
            for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Click(shell, "completion-menu"); Require(shell.Screen == "menu", "completion Menu routing");
            })) yield break;
            for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Click(shell, "menu-back"); advance(10); shell.TickCompletion();
                Require(shell.Q("completion-actions").style.display.value == DisplayStyle.None, "Menu returns to static completed board");
                Debug.Log("COMPLETION_REVIEW_PASS locale=" + shell.Locale + " viewport=" + Screen.width + "x" + Screen.height + " mouse+touch+Hint");
            })) yield break;
            yield return new WaitForEndOfFrame();
            Application.Quit(0);
        }
        private static void Click(GameShellView shell, string name)
        {
            var button = shell.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy, "enabled button " + name);
            var position = button.worldBound.center; var picked = shell.panel.Pick(position);
            Require(picked == button || (picked != null && button.Contains(picked)), "panel picks " + name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { down.target = button; button.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { up.target = button; button.SendEvent(up); }
        }
        private static void Tap(GameShellView shell, BoardCoordinate coordinate, bool touch)
        {
            var grid = shell.BoardView.Q("board-grid");
            var position = grid.LocalToWorld(shell.BoardView.Layout.GetCellRect(coordinate).center);
            var contact = new Touch { fingerId = 0, position = position, rawPosition = position, phase = TouchPhase.Began, tapCount = 1 };
            using (var down = touch ? PointerDownEvent.GetPooled(contact, EventModifiers.None) :
                PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { down.target = grid; grid.SendEvent(down); }
            contact.phase = TouchPhase.Ended;
            using (var up = touch ? PointerUpEvent.GetPooled(contact, EventModifiers.None) :
                PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { up.target = grid; grid.SendEvent(up); }
        }
        private static float Delta(Rect a, Rect b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.width - b.width), Mathf.Abs(a.height - b.height));
        private static bool Check(Action action)
        { try { action(); return true; } catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; } }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Completion review failed: " + message); }
    }
}
