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
    // Issue #26: opt-in native mounted-panel evidence, not a physical touch test.
    internal static class TutorialReviewChecks
    {
        public static IEnumerator Run(GameShellView shell, string captures)
        {
            for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-start"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Require(shell.Tutorial?.Step == TutorialStep.Welcome, "first Start enters tutorial");
                Require(shell.Q<Button>("action-continue").style.borderLeftWidth.value == 3, "welcome Continue highlighted");
            })) yield break;
            yield return Capture(shell, captures, "welcome");
            while (shell.Tutorial.CanContinue)
            {
                if (!Check(() => Click(shell, "action-continue"))) yield break;
                yield return new WaitForEndOfFrame();
                if (!Check(() =>
                {
                    if (shell.Tutorial.DemoMark.HasValue)
                    {
                        Require(shell.ActiveSession.Marks.Values.All(mark => mark == PlayerMark.Empty), "demo leaves all player marks Empty");
                        var expected = shell.Tutorial.DemoMark.Value == PlayerMark.X ? BoardCellKind.X
                            : shell.Tutorial.DemoMark.Value == PlayerMark.Paw ? BoardCellKind.Paw : BoardCellKind.Empty;
                        Require(shell.BoardView.Q<VisualElement>("cell-" + shell.Tutorial.DemoCoordinate.Value.Row + "-" + shell.Tutorial.DemoCoordinate.Value.Column).Q<BoardSymbolElement>().Kind == expected, "demo symbol matches lesson");
                        Require(shell.BoardView.Q<Label>("completion-status").text.Contains("<b>"), "state keyword bold");
                    }
                })) yield break;
                yield return Capture(shell, captures, "intro-" + shell.Tutorial.Step);
            }
            if (!Check(() =>
            {
                Require(shell.Tutorial.Step == TutorialStep.Practice, "practice after information");
                Tap(shell, 4, 4); // Off-target input is ignored.
                Require(shell.ActiveSession.Marks.Values.All(mark => mark == PlayerMark.Empty), "off-target no mutation");
                var p = Point(shell, 0, 0); Send(Grid(shell), p, TouchPhase.Began);
                shell.CancelInteraction(); Send(Grid(shell), p, TouchPhase.Ended);
                Require(shell.ActiveSession.GetMark(new BoardCoordinate(0, 0)) == PlayerMark.Empty, "cancel no tap");
                Tap(shell, 0, 0); Tap(shell, 0, 0);
                Require(shell.Tutorial.Step == TutorialStep.Practice, "practice requires complete cycle");
                Tap(shell, 0, 0);
                Require(shell.Tutorial.Step == TutorialStep.ZeroColumn, "practice full cycle");
            })) yield break;
            yield return Capture(shell, captures, "zero-column");
            if (!Check(() =>
            {
                var grid = Grid(shell); var start = Point(shell, 0, 0); var end = Point(shell, 4, 0);
                Send(grid, start, TouchPhase.Began); Send(grid, end, TouchPhase.Moved);
                Require(shell.Tutorial.Step == TutorialStep.ZeroColumn, "no advance during swipe");
                Send(grid, end, TouchPhase.Ended);
                Require(shell.Tutorial.Step == TutorialStep.ZeroRow, "advance on release even without new marks");
                Require(shell.ActiveSession.GetMark(new BoardCoordinate(0, 1)) == PlayerMark.Empty, "no next-step spill");
            })) yield break;
            yield return Capture(shell, captures, "zero-row");
            if (!Check(() => { Drag(shell, 0, 1, 0, 4); Require(shell.Tutorial.Step == TutorialStep.FirstPaw, "zero row skips fixed/intersection"); })) yield break;
            yield return Capture(shell, captures, "first-paw");
            if (!Check(() => { Tap(shell, 1, 4); Tap(shell, 1, 4); Require(shell.Tutorial.Step == TutorialStep.FirstColumnX, "first paw"); })) yield break;
            yield return Capture(shell, captures, "column-x");
            if (!Check(() => { Drag(shell, 2, 4, 4, 4); Require(shell.Tutorial.Step == TutorialStep.SecondPaw, "column quota"); })) yield break;
            yield return Capture(shell, captures, "second-paw");
            if (!Check(() => { Tap(shell, 4, 2); Tap(shell, 4, 2); Require(shell.Tutorial.Step == TutorialStep.SecondRowX, "second paw"); })) yield break;
            yield return Capture(shell, captures, "row-x");
            if (!Check(() => { Drag(shell, 4, 1, 4, 3); Require(shell.Tutorial.Step == TutorialStep.DiagonalPaw, "swipe across paw"); Require(shell.ActiveSession.GetMark(new BoardCoordinate(4, 2)) == PlayerMark.Paw, "existing paw preserved"); })) yield break;
            yield return Capture(shell, captures, "diagonal");
            if (!Check(() => { Tap(shell, 2, 1); Tap(shell, 2, 1); Require(shell.Tutorial.Step == TutorialStep.FourthPaw, "diagonal paw"); })) yield break;
            yield return Capture(shell, captures, "two-paw-row");
            if (!Check(() => { Tap(shell, 2, 3); Tap(shell, 2, 3); Require(shell.Tutorial.Step == TutorialStep.SingleX, "two paw row"); Tap(shell, 3, 3); Require(shell.Tutorial.Step == TutorialStep.FinalPaw, "single X cleanup"); })) yield break;
            yield return Capture(shell, captures, "last-paw");
            if (!Check(() =>
            {
                Tap(shell, 3, 1); Tap(shell, 3, 1);
                Require(shell.Tutorial.Step == TutorialStep.Independent && !shell.ActiveSession.IsCompleted, "guided solve enters independent practice");
                Require(shell.ActiveSession.Marks.Values.All(mark => mark == PlayerMark.Empty), "same board reset to Empty");
                Require(shell.BoardView.InteractionEnabled && shell.Q<Button>("action-play") == null, "independent input enabled without random Play");
                Require(shell.Q<Button>("action-hint") == null && shell.Q<Button>("action-clear") == null, "no normal commands during tutorial");
            })) yield break;
            yield return Capture(shell, captures, "independent");
            if (!Check(() =>
            {
                var session = shell.ActiveSession;
                var locale = shell.Locale;
                var hadLanguage = PlayerPrefs.HasKey(LanguagePreference.Key);
                var previousLanguage = PlayerPrefs.GetString(LanguagePreference.Key);
                try
                {
                    shell.ShowMenu(); shell.SelectLanguage(locale == "en" ? "pt-BR" : "en"); shell.BackToGame();
                    Require(shell.Tutorial.Step == TutorialStep.Independent && shell.ActiveSession == session && shell.BoardView.InteractionEnabled, "independent survives menu/language return");
                    shell.ShowMenu(); shell.SelectLanguage(locale); shell.BackToGame();
                }
                finally
                {
                    if (hadLanguage) PlayerPrefs.SetString(LanguagePreference.Key, previousLanguage); else PlayerPrefs.DeleteKey(LanguagePreference.Key);
                    PlayerPrefs.Save();
                }
            })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                Tap(shell, 0, 1); Tap(shell, 0, 1); // A wrong extra paw must prevent completion.
                Drag(shell, 0, 0, 4, 0); // Free input supports the same ordinary swipe rules.
                foreach (var paw in shell.ActiveSession.Puzzle.SolutionPaws.ToArray())
                { Tap(shell, paw.Row, paw.Column); Tap(shell, paw.Row, paw.Column); }
                Require(shell.Tutorial.Step == TutorialStep.Independent && !shell.ActiveSession.IsCompleted, "wrong extra paw prevents victory");
                Tap(shell, 0, 1); // Correct the hypothesis; non-paw cells may remain Empty.
                Require(shell.Tutorial.IsComplete && shell.ActiveSession.IsCompleted, "independent solve completes tutorial");
                var marks = shell.ActiveSession.Marks.ToArray(); Tap(shell, 3, 1);
                Require(shell.ActiveSession.Marks.SequenceEqual(marks) && !shell.BoardView.InteractionEnabled, "completed board locked");
            })) yield break;
            yield return Capture(shell, captures, "complete");
            if (!Check(() => Click(shell, "action-play"))) yield break;
            yield return Ready(shell);
            if (!Check(() => Require(shell.Tutorial == null && shell.ActiveSession == shell.Loop.Session && shell.ActiveSession != null, "Play opens random"))) yield break;
            if (!Check(() => Click(shell, "action-menu"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-tutorial"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => { Require(shell.Tutorial.Step == TutorialStep.Welcome && shell.ActiveSession.Marks.Values.All(mark => mark == PlayerMark.Empty), "manual replay fresh"); })) yield break;
            while (shell.Tutorial.CanContinue)
            {
                if (!Check(() => Click(shell, "action-continue"))) yield break;
                yield return new WaitForEndOfFrame();
            }
            if (!Check(() =>
            {
                Tap(shell, 0, 0); Tap(shell, 0, 0); Tap(shell, 0, 0);
                var grid = Grid(shell); var start = Point(shell, 0, 0); var partial = Point(shell, 2, 0);
                Send(grid, start, TouchPhase.Began); Send(grid, partial, TouchPhase.Moved);
                Require(grid.HasPointerCapture(PointerId.touchPointerIdBase), "active swipe before Skip");
                Click(shell, "action-skip"); Send(grid, partial, TouchPhase.Ended);
                Require(!grid.HasPointerCapture(PointerId.touchPointerIdBase), "Skip cancels active swipe");
            })) yield break;
            yield return Ready(shell);
            if (!Check(() => Require(shell.Tutorial == null && shell.ActiveSession != null && shell.ActiveSession.Marks.Values.All(mark => mark == PlayerMark.Empty), "Skip opens random"))) yield break;
            if (!Check(() => Click(shell, "action-menu"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-start"))) yield break;
            yield return Ready(shell);
            if (!Check(() => { Require(shell.Tutorial == null && shell.ActiveSession != null, "first play not repeated"); Debug.Log("TUTORIAL_REVIEW_PASS locale=" + shell.Locale + " source=synthetic-touch"); })) yield break;
            Application.Quit(0);
        }
        private static IEnumerator Ready(GameShellView shell)
        {
            for (var i = 0; i < 120 && shell.Modal == "loading"; i++) yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        private static IEnumerator Capture(GameShellView shell, string directory, string name)
        {
            for (var i = 0; i < 2; i++) yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                var board = Grid(shell); var layout = shell.BoardView.Layout;
                var center = board.LocalToWorld(new Vector2(layout.ClueGutter + layout.GridSize / 2, layout.ClueGutter + layout.GridSize / 2));
                Require(Vector2.Distance(center, board.parent.worldBound.center) < .6f, "tile grid centered");
                var label = shell.BoardView.Q<Label>("completion-status");
                var measured = label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Require(measured.y <= label.contentRect.height + 1, "instruction fits header: " + name + " needs=" + measured.y + " has=" + label.contentRect.height);
                Require(label.worldBound.yMax <= board.Q<Label>("column-clue-0").worldBound.yMin + 1, "instruction stays above column clues: " + name);
                if (string.IsNullOrEmpty(directory)) return;
                Require(Path.IsPathRooted(directory), "absolute capture directory"); Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            })) yield break;
            yield return new WaitForSecondsRealtime(.15f);
        }
        private static VisualElement Grid(GameShellView shell) => shell.BoardView.Q<VisualElement>("board-grid");
        private static Vector2 Point(GameShellView shell, int row, int column) => Grid(shell).LocalToWorld(shell.BoardView.Layout.GetCellRect(new BoardCoordinate(row, column)).center);
        private static void Tap(GameShellView shell, int row, int column) { var grid = Grid(shell); var point = Point(shell, row, column); Send(grid, point, TouchPhase.Began); Send(grid, point, TouchPhase.Ended); }
        private static void Drag(GameShellView shell, int r1, int c1, int r2, int c2) { var grid = Grid(shell); Send(grid, Point(shell, r1, c1), TouchPhase.Began); Send(grid, Point(shell, r2, c2), TouchPhase.Moved); Send(grid, Point(shell, r2, c2), TouchPhase.Ended); }
        private static void Send(VisualElement target, Vector2 position, TouchPhase phase)
        {
            var touch = new Touch { fingerId = 0, position = position, rawPosition = position, phase = phase, type = TouchType.Direct, pressure = 1, maximumPossiblePressure = 1 };
            if (phase == TouchPhase.Began) { using var e = PointerDownEvent.GetPooled(touch, EventModifiers.None); e.target = target; target.SendEvent(e); }
            else if (phase == TouchPhase.Ended) { using var e = PointerUpEvent.GetPooled(touch, EventModifiers.None); e.target = target; target.SendEvent(e); }
            else { using var e = PointerMoveEvent.GetPooled(touch, EventModifiers.None); e.target = target; target.SendEvent(e); }
        }
        private static void Click(GameShellView shell, string name)
        {
            var button = shell.Q<Button>(name); Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0, "mounted " + name);
            var position = button.worldBound.center; var picked = shell.panel.Pick(position);
            Require(picked == button || (picked != null && button.Contains(picked)), "button picked " + name);
            using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position })) { e.target = button; button.SendEvent(e); }
            using (var e = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position })) { e.target = button; button.SendEvent(e); }
        }
        private static bool Check(Action action) { try { action(); return true; } catch (Exception e) { Debug.LogException(e); Application.Quit(1); return false; } }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Tutorial review failed: " + message); }
    }
}
