using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Opt-in mounted UI checks. Synthetic button presses measure this player, not human touch latency.
    internal static class GameLoopReviewChecks
    {
        public static IEnumerator Run(GameShellView shell, string captures)
        {
            for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame();
            if (!Check(() => Require(shell.Screen == "menu", "starts in menu"))) yield break;
            if (!Check(() =>
            {
                foreach (var id in new[] { "easy", "medium", "hard" })
                    Require(shell.Q<Button>("menu-difficulty-" + id) == null, "Play has no difficulty choice");
            })) yield break;
            yield return Capture(captures, "menu");
            if (!Check(() => Click(shell, "menu-rules"))) yield break;
            yield return Capture(captures, "rules");
            if (!Check(() => { Require(shell.Modal == "rules", "rules open"); Click(shell, "modal-back"); })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-start"))) yield break;
            yield return Ready(shell);
            if (!Check(() => Require(shell.Loop.Session != null && shell.Screen == "play" &&
                shell.Loop.Session.Puzzle.Size == shell.Loop.SessionDifficulty.Size, "random difficulty start"))) yield break;
            var firstMs = Elapsed(shell);
            yield return Capture(captures, "play");
            if (!Check(() => CheckGridCentered(shell))) yield break;
            var session = shell.Loop.Session;
            var puzzle = session.Puzzle;
            var protectedCells = session.ProtectedCells.ToArray();
            if (!Check(() =>
            {
                session.Tap(session.Marks.Keys.First()); shell.BoardView.Refresh();
                Click(shell, "action-clear");
                Require(session.Marks.Values.All(mark => mark == PlayerMark.Empty), "Clear resets marks");
                Require(ReferenceEquals(puzzle, session.Puzzle) && session.ProtectedCells.SequenceEqual(protectedCells), "Clear preserves clues/protection");
                Click(shell, "action-hint");
                if (shell.UseInlineHints)
                {
                    Require(shell.Modal == null && shell.BoardView.InteractionEnabled, "immediate hint keeps board playable");
                    CheckNoHintExplanation(shell);
                    Require(session.Marks.Values.Count(mark => mark != PlayerMark.Empty) == 1, "Hint applies exactly one mark");
                    CheckGridCentered(shell);
                }
                else
                {
                    Require(shell.Modal == "hint" && !shell.BoardView.InteractionEnabled, "legacy hint blocks input");
                    var marks = session.Marks.ToArray(); shell.ClearBoard();
                    Require(session.Marks.SequenceEqual(marks), "modal suppresses Clear");
                }
            })) yield break;
            yield return Capture(captures, "hint");
            if (!Check(() =>
            {
                if (!shell.UseInlineHints) return;
                var before = session.Marks.ToArray();
                var coordinate = session.Marks.First(pair => pair.Value == PlayerMark.Empty).Key;
                TapCell(shell, coordinate); TapCell(shell, coordinate); TapCell(shell, coordinate);
                Require(session.Marks.SequenceEqual(before) && shell.Modal == null, "board taps continue after Hint");
                CheckNoHintExplanation(shell);
                CheckGridCentered(shell);
            })) yield break;
            if (!Check(() => { if (!shell.UseInlineHints) Click(shell, "modal-back"); })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => { Click(shell, "action-menu"); Require(ReferenceEquals(session, shell.Loop.Session), "Menu keeps board"); })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-back"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() =>
            {
                if (!shell.UseInlineHints) return;
                CheckNoHintExplanation(shell);
                shell.ClearBoard();
                Require(session.Marks.Values.All(mark => mark == PlayerMark.Empty), "Clear resets after immediate hint");
            })) yield break;
            for (var i = 0; i < Math.Max(30, session.Puzzle.Size * session.Puzzle.Size) && !session.IsCompleted; i++)
            {
                if (!Check(() => shell.RequestHint())) yield break;
                if (!session.IsCompleted && !shell.UseInlineHints) shell.BackToGame();
                yield return new WaitForEndOfFrame();
            }
            if (!Check(() =>
            {
                Require(session.IsCompleted && shell.Modal == null && shell.Screen == "play", "completion stays on grid");
                Require(!shell.BoardView.InteractionEnabled, "completed view read-only");
                Require(!string.IsNullOrEmpty(shell.Q<Label>("completion-status").text), "inline completion message");
                if (shell.UseInlineHints)
                {
                    CheckNoHintExplanation(shell);
                    Require(shell.Q<Button>("completion-hint").resolvedStyle.display == DisplayStyle.None, "completed hint has no explanation action");
                }
                else
                {
                    Click(shell, "completion-hint");
                    Require(shell.Modal == "last-hint", "legacy final hint explanation is opt-in");
                }
            })) yield break;
            if (!shell.UseInlineHints) yield return Capture(captures, "last-hint");
            if (!Check(() =>
            {
                if (!shell.UseInlineHints) Click(shell, "modal-back");
                Require(!shell.Q<Button>("action-hint").enabledSelf && !shell.Q<Button>("action-clear").enabledSelf, "completed controls disabled");
                Require(shell.Q<Button>("action-new-game").enabledSelf && shell.Q<Button>("action-menu").enabledSelf, "new/menu remain active");
                var before = session.Marks.ToArray(); shell.RequestHint(); shell.ClearBoard();
                Require(session.Marks.SequenceEqual(before), "completed read-only");
            })) yield break;
            yield return Capture(captures, "completed");
            var samples = new List<double>();
            for (var i = 0; i < 20; i++)
            {
                var previous = shell.Loop.RequestedSeed;
                if (!Check(() => Click(shell, "action-new-game"))) yield break;
                yield return Ready(shell);
                if (!Check(() =>
                {
                    Require(shell.Loop.Session != null && shell.Loop.RequestedSeed != previous, "new seed/ready");
                    Require(shell.Loop.Session.Puzzle.Size == shell.Loop.SessionDifficulty.Size &&
                        shell.Loop.DifficultySelection.History.ConsecutiveStarts <= 2, "Next board respects difficulty run");
                    Require(string.IsNullOrEmpty(shell.Q<Label>("completion-status").text), "New Game clears completion");
                    Require(shell.Q<Button>("completion-hint").resolvedStyle.display == DisplayStyle.None, "New Game clears final hint");
                    if (shell.UseInlineHints) CheckNoHintExplanation(shell);
                    CheckGridCentered(shell);
                })) yield break;
                samples.Add(Elapsed(shell));
                UnityEngine.Debug.Log("GAME_LOOP_READY seed=" + shell.Loop.RequestedSeed + " ms=" + samples.Last().ToString("F3", CultureInfo.InvariantCulture));
            }
            // Separate from generation timing: exercise the actual board adapter
            // through mounted pointer events so completion is not only hint-tested.
            var tappedSession = shell.Loop.Session;
            foreach (var paw in tappedSession.Puzzle.SolutionPaws)
            {
                if (!Check(() => { TapCell(shell, paw); TapCell(shell, paw); })) yield break;
                yield return new WaitForEndOfFrame();
            }
            if (!Check(() =>
            {
                Require(tappedSession.IsCompleted && shell.Modal == null && shell.Screen == "play", "pointer completion stays on grid");
                Require(!shell.BoardView.InteractionEnabled, "pointer completion disables view input");
                Require(shell.Q<Button>("completion-hint").resolvedStyle.display == DisplayStyle.None, "pointer completion has no final-hint action");
                if (shell.UseInlineHints) CheckNoHintExplanation(shell);
                var before = tappedSession.Marks.ToArray();
                TapCell(shell, tappedSession.Puzzle.SolutionPaws.First());
                Require(tappedSession.Marks.SequenceEqual(before), "completed pointer tap has no effect");
                Require(!shell.Q<Button>("action-hint").enabledSelf && !shell.Q<Button>("action-clear").enabledSelf, "pointer completion disables Hint/Clear");
            })) yield break;
            yield return Capture(captures, "completed-tap");
            samples.Sort();
            var report = "GAME_LOOP_REVIEW_PASS locale=" + shell.Locale + " platform=" + Application.platform +
                " difficulty=" + shell.Loop.SessionDifficulty.Id + " size=" + shell.Loop.SessionDifficulty.Size +
                " hintFlow=" + (shell.UseInlineHints ? "immediate" : "legacy") +
                " source=synthetic-button firstMs=" + firstMs.ToString("F3", CultureInfo.InvariantCulture) +
                " requests=20 medianMs=" + samples[9].ToString("F3", CultureInfo.InvariantCulture) +
                " p95Ms=" + samples[18].ToString("F3", CultureInfo.InvariantCulture) +
                " maxMs=" + samples[19].ToString("F3", CultureInfo.InvariantCulture);
            if (!Check(() => { UnityEngine.Debug.Log(report); File.WriteAllText(Path.Combine(Application.persistentDataPath, "issue8-game-loop.txt"), report); })) yield break;
            Application.Quit(0);
        }
        private static IEnumerator Ready(GameShellView shell)
        {
            for (var frame = 0; frame < 120 && shell.Modal == "loading"; frame++) yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        private static void CheckNoHintExplanation(GameShellView shell)
        {
            Require(shell.Q<Button>("action-why") == null, "no Why action");
            Require(shell.Q<VisualElement>("hint-explanation-scroll") == null && shell.Q<Label>("hint-explanation") == null, "no inline explanation UI");
        }
        private static void CheckGridCentered(GameShellView shell)
        {
            var board = shell.BoardView.Q<VisualElement>("board-grid");
            var gridCenter = board.LocalToWorld(new Vector2(shell.BoardView.Layout.ClueGutter + shell.BoardView.Layout.GridSize / 2,
                shell.BoardView.Layout.ClueGutter + shell.BoardView.Layout.GridSize / 2));
            Require(Vector2.Distance(gridCenter, board.parent.worldBound.center) < .6f, "actual tile grid centered");
        }
        private static double Elapsed(GameShellView shell) =>
            (Stopwatch.GetTimestamp() - shell.NewGameStartedAtTimestamp) * 1000.0 / Stopwatch.Frequency;
        private static IEnumerator Capture(string directory, string name)
        {
            if (string.IsNullOrEmpty(directory)) yield break;
            for (var frame = 0; frame < 2; frame++) yield return new WaitForEndOfFrame();
            if (!Check(() => { if (!Path.IsPathRooted(directory)) throw new ArgumentException("Capture directory must be absolute."); Directory.CreateDirectory(directory); ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png")); })) yield break;
            yield return new WaitForSecondsRealtime(.3f);
        }
        private static void Click(GameShellView shell, string name)
        {
            var button = shell.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0, "mounted button " + name);
            var position = button.worldBound.center;
            var picked = shell.panel.Pick(position);
            Require(picked == button || (picked != null && button.Contains(picked)), "panel picks button " + name);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { down.target = button; button.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { up.target = button; button.SendEvent(up); }
        }
        private static void TapCell(GameShellView shell, BoardCoordinate coordinate)
        {
            var board = shell.BoardView.Q<VisualElement>("board-grid");
            var position = board.LocalToWorld(shell.BoardView.Layout.GetCellRect(coordinate).center);
            var picked = shell.panel.Pick(position);
            Require(picked == board || (picked != null && board.Contains(picked)), "panel picks tile " + coordinate);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { down.target = board; board.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { up.target = board; board.SendEvent(up); }
        }
        private static bool Check(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { UnityEngine.Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Game loop review failed: " + message); }
    }
}
