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
    // Issue #48: explicit mounted diagnostics; fake time never enters ordinary play.
    internal static class HintIdleReviewChecks
    {
        public static IEnumerator Run(GameShellView shell, Action<double> advance, string locale, string captures)
        {
            for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame();
            if (!Check(() => Require(shell.StartNewGame(), "ordinary board starts"))) yield break;
            for (var i = 0; i < 6; i++) yield return new WaitForEndOfFrame();
            var hint = shell.Q<Button>("action-hint");
            var grid = shell.BoardView.Q<VisualElement>("board-grid");
            var buttonBounds = hint.worldBound;
            var contentBounds = hint.contentRect;
            var clearBounds = shell.Q<Button>("action-clear").worldBound;
            var gridBounds = grid.worldBound;
            var label = hint.text;
            if (!Check(() =>
            {
                Require(shell.panel != null && buttonBounds.height >= 48 && gridBounds.width > 0, "mounted touch targets");
                shell.TickHintAttention();
                advance(5.999); shell.TickHintAttention(); Require(!Attention(shell), "no attention before six seconds");
                advance(.001); shell.TickHintAttention(); Require(Attention(shell), "attention at six seconds");
                Require(hint.style.unityFontStyleAndWeight.value == FontStyle.Bold, "non-color font emphasis");
            })) yield break;
            for (var i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                var clear = shell.Q<Button>("action-clear").worldBound;
                var delta = Mathf.Max(Delta(hint.worldBound, buttonBounds), Delta(hint.contentRect, contentBounds),
                    Delta(clear, clearBounds), Delta(grid.worldBound, gridBounds));
                // Sub-pixel float noise is not a layout change; a one-unit resize still fails.
                Require(delta <= .01f,
                    "attention preserves button and grid geometry: button " + buttonBounds + " -> " + hint.worldBound +
                    "; content " + contentBounds + " -> " + hint.contentRect + "; Clear " + clearBounds + " -> " + clear +
                    "; grid " + gridBounds + " -> " + grid.worldBound + "; max delta=" + delta.ToString("R"));
                Debug.Log("HINT_IDLE_GEOMETRY_MAX_DELTA " + delta.ToString("R"));
                Require(hint.text == label, "localized label unchanged");
                if (!string.IsNullOrEmpty(captures))
                {
                    Require(Path.IsPathRooted(captures), "absolute capture directory"); Directory.CreateDirectory(captures);
                    ScreenCapture.CaptureScreenshot(Path.Combine(captures, "hint-idle.png"));
                }
            })) yield break;
            yield return new WaitForEndOfFrame();
            foreach (var touch in new[] { false, true })
            {
                if (!Check(() =>
                {
                    var session = shell.ActiveSession;
                    var marks = session.Marks.ToArray();
                    var arrow = session.Puzzle.Arrows.Keys.First();
                    advance(6); shell.TickHintAttention();
                    Send(shell, arrow, false, TouchPhase.Stationary);
                    Require(Attention(shell), "passive mouse movement does not reset attention");
                    Send(shell, arrow, touch, TouchPhase.Began);
                    Require(!Attention(shell) && shell.BoardView.HasActiveInteraction, "arrow press clears attention");
                    if (touch)
                    {
                        var other = session.Marks.Keys.First();
                        Send(shell, other, true, TouchPhase.Began, 1); Send(shell, other, true, TouchPhase.Ended, 1);
                        Send(shell, other, false, TouchPhase.Began); Send(shell, other, false, TouchPhase.Ended);
                        Require(shell.BoardView.HasActiveInteraction, "secondary touch and mouse cannot end owning activity");
                    }
                    advance(20); shell.TickHintAttention(); Require(!Attention(shell), "no idle attention while arrow held");
                    Send(shell, arrow, touch, TouchPhase.Ended);
                    Require(!shell.BoardView.HasActiveInteraction && session.Marks.SequenceEqual(marks), "arrow release never marks");
                    advance(5.999); shell.TickHintAttention(); Require(!Attention(shell), "full interval after release");
                    advance(.002); shell.TickHintAttention(); Require(Attention(shell), "attention resumes after release");
                    if (session.ProtectedCells.Count > 0)
                    {
                        var protectedCell = session.ProtectedCells.First();
                        Send(shell, protectedCell, touch, TouchPhase.Began);
                        advance(20); shell.TickHintAttention(); Require(!Attention(shell), "protected attempt suppresses idle while held");
                        Send(shell, protectedCell, touch, TouchPhase.Ended);
                        Require(session.Marks.SequenceEqual(marks), "protected attempt never marks");
                    }
                    var empty = session.Marks.First(pair => pair.Value == PlayerMark.Empty).Key;
                    Send(shell, empty, touch, TouchPhase.Began);
                    advance(20); shell.TickHintAttention(); Require(!Attention(shell), "no attention during ordinary long press");
                    Send(shell, empty, touch, TouchPhase.Ended);
                    Require(session.GetMark(empty) == PlayerMark.X, "normal tap cycle preserved");
                    Send(shell, empty, touch, TouchPhase.Began); shell.CancelInteraction();
                    Require(!shell.BoardView.HasActiveInteraction, "cancel releases activity ownership");
                    advance(6); shell.TickHintAttention(); Require(Attention(shell), "full interval after cancellation");
                    Click(shell, "action-hint"); Require(!Attention(shell) && shell.Modal == null, "mounted Hint clears attention without modal");
                    Click(shell, "action-clear"); Require(session.Marks.Values.All(mark => mark == PlayerMark.Empty), "Clear preserves input behavior");
                    advance(6); shell.TickHintAttention(); Require(Attention(shell), "Clear restarts interval");
                    var line = session.Marks.Keys.Where(session.CanInteract).GroupBy(cell => cell.Row)
                        .First(row => row.Count() >= 2).OrderBy(cell => cell.Column).ToArray();
                    Send(shell, line.First(), touch, TouchPhase.Began);
                    Send(shell, line.Last(), touch, TouchPhase.Moved);
                    advance(20); shell.TickHintAttention(); Require(!Attention(shell), "continuous drag suppresses idle attention");
                    Send(shell, line.Last(), touch, TouchPhase.Ended);
                    Require(line.All(cell => session.GetMark(cell) == PlayerMark.X), "swept drag preserved");
                    advance(6); shell.TickHintAttention(); Require(Attention(shell), "full interval after drag");
                    shell.ClearBoard();
                })) yield break;
                yield return new WaitForEndOfFrame();
            }
            var heldArrow = shell.ActiveSession.Puzzle.Arrows.Keys.First();
            if (!Check(() => { Send(shell, heldArrow, true, TouchPhase.Began); advance(6); })) yield break;
            var ticked = false;
            var sentinel = shell.schedule.Execute(() => ticked = true).StartingIn(80);
            var deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (!ticked && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            sentinel.Pause();
            if (!Check(() =>
            {
                Require(ticked, "mounted scheduler advances");
                Require(shell.BoardView.Q<VisualElement>("cell-" + heldArrow.Row + "-" + heldArrow.Column)
                    .style.borderLeftWidth.value == 3 && !Attention(shell), "active arrow outline suppresses idle attention");
                Send(shell, heldArrow, true, TouchPhase.Ended);
                advance(6); shell.TickHintAttention(); Require(Attention(shell), "idle resumes after visible arrow hold");
            })) yield break;
            if (!Check(() =>
            {
                Click(shell, "action-menu"); advance(60); shell.TickHintAttention(); Require(!Attention(shell), "menu discards idle time");
            })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Click(shell, "menu-back"); shell.TickHintAttention(); Require(!Attention(shell), "return starts fresh");
                advance(6); shell.TickHintAttention(); Require(Attention(shell), "return interval");
                shell.ShowRules(); Require(!Attention(shell), "modal immediately removes emphasis");
                advance(60); shell.TickHintAttention(); shell.BackToGame();
                advance(5.999); shell.TickHintAttention(); Require(!Attention(shell), "no modal time accumulation");
                advance(.002); shell.TickHintAttention(); Require(Attention(shell), "full post-modal interval");
                shell.ShowMenu(); shell.SelectLanguage(locale); shell.BackToGame(); shell.TickHintAttention();
                Require(!Attention(shell) && hint.style.borderLeftWidth.value == 1, "old button restored after locale rebuild");
                for (var i = 0; i < 80 && !shell.ActiveSession.IsCompleted; i++) shell.RequestHint();
                Require(shell.ActiveSession.IsCompleted && !shell.Q<Button>("action-hint").enabledSelf, "final Hint completes and disables assistance");
                advance(60); shell.TickHintAttention(); Require(!Attention(shell), "completed board never nudges");
                Debug.Log("HINT_IDLE_REVIEW_PASS locale=" + locale + " difficulty=" + shell.Loop.SessionDifficulty.Id +
                    " width=" + Screen.width + " height=" + Screen.height);
            })) yield break;
            Application.Quit(0);
        }

        private static bool Attention(GameShellView shell) => shell.Q<Button>("action-hint")?.style.borderLeftWidth.value > 1;
        private static float Delta(Rect a, Rect b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y),
            Mathf.Abs(a.width - b.width), Mathf.Abs(a.height - b.height));
        private static void Click(GameShellView shell, string name)
        {
            var button = shell.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0, "enabled mounted button " + name);
            var position = button.worldBound.center;
            var picked = shell.panel.Pick(position);
            Require(picked == button || (picked != null && button.Contains(picked)), "panel picks " + name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { down.target = button; button.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { up.target = button; button.SendEvent(up); }
        }
        private static void Send(GameShellView shell, BoardCoordinate coordinate, bool touch, TouchPhase phase, int finger = 0)
        {
            var grid = shell.BoardView.Q<VisualElement>("board-grid");
            var position = grid.LocalToWorld(shell.BoardView.Layout.GetCellRect(coordinate).center);
            var mouse = new Event { button = 0, mousePosition = position,
                type = phase == TouchPhase.Began ? EventType.MouseDown : phase == TouchPhase.Ended ? EventType.MouseUp :
                    phase == TouchPhase.Stationary ? EventType.MouseMove : EventType.MouseDrag };
            var contact = new Touch { fingerId = finger, position = position, rawPosition = position, phase = phase, tapCount = 1 };
            if (phase == TouchPhase.Began)
            {
                using (var evt = touch ? PointerDownEvent.GetPooled(contact, EventModifiers.None) : PointerDownEvent.GetPooled(mouse))
                { evt.target = grid; grid.SendEvent(evt); }
            }
            else if (phase == TouchPhase.Ended)
            {
                using (var evt = touch ? PointerUpEvent.GetPooled(contact, EventModifiers.None) : PointerUpEvent.GetPooled(mouse))
                { evt.target = grid; grid.SendEvent(evt); }
            }
            else
            {
                using (var evt = touch ? PointerMoveEvent.GetPooled(contact, EventModifiers.None) : PointerMoveEvent.GetPooled(mouse))
                { evt.target = grid; grid.SendEvent(evt); }
            }
        }
        private static bool Check(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Hint idle review failed: " + message); }
    }
}
