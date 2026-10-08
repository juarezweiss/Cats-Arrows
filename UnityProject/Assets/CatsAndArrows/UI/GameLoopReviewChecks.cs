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
            yield return Capture(captures, "menu");
            if (!Check(() => Click(shell, "menu-rules"))) yield break;
            yield return Capture(captures, "rules");
            if (!Check(() => { Require(shell.Modal == "rules", "rules open"); Click(shell, "modal-back"); })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-start"))) yield break;
            yield return Ready(shell);
            if (!Check(() => Require(shell.Loop.Session != null && shell.Screen == "play", "Easy start"))) yield break;
            var firstMs = Elapsed(shell);
            yield return Capture(captures, "play");
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
                Require(shell.Modal == "hint" && !shell.BoardView.InteractionEnabled, "hint blocks input");
                var marks = session.Marks.ToArray(); shell.ClearBoard();
                Require(session.Marks.SequenceEqual(marks), "modal suppresses Clear");
            })) yield break;
            yield return Capture(captures, "hint");
            if (!Check(() => Click(shell, "modal-back"))) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => { Click(shell, "action-menu"); Require(ReferenceEquals(session, shell.Loop.Session), "Menu keeps board"); })) yield break;
            yield return new WaitForEndOfFrame();
            if (!Check(() => Click(shell, "menu-back"))) yield break;
            for (var i = 0; i < 30 && !session.IsCompleted; i++)
            {
                if (!Check(() => shell.RequestHint())) yield break;
                if (!session.IsCompleted) shell.BackToGame();
                yield return new WaitForEndOfFrame();
            }
            if (!Check(() => Require(session.IsCompleted && shell.Modal == "victory", "immediate hint victory"))) yield break;
            yield return Capture(captures, "victory");
            if (!Check(() =>
            {
                Click(shell, "modal-back");
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
                if (!Check(() => Require(shell.Loop.Session != null && shell.Loop.RequestedSeed != previous, "new seed/ready"))) yield break;
                samples.Add(Elapsed(shell));
                UnityEngine.Debug.Log("GAME_LOOP_READY seed=" + shell.Loop.RequestedSeed + " ms=" + samples.Last().ToString("F3", CultureInfo.InvariantCulture));
            }
            samples.Sort();
            var report = "GAME_LOOP_REVIEW_PASS locale=" + shell.Locale + " platform=" + Application.platform +
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
        private static bool Check(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { UnityEngine.Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Game loop review failed: " + message); }
    }
}
