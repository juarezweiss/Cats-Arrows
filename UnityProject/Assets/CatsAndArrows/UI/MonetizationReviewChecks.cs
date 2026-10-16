#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #30: explicitly mounted diagnostic, never active in a normal player.
    public static class MonetizationReviewChecks
    {
        public static IEnumerator Run(GameShellView shell, string directory)
        {
            yield return Settle();
            if (!Check(() => {
                Require(shell.panel != null, "mounted panel");
                CheckButton(shell, "menu-remove-ads"); CheckButton(shell, "menu-restore");
            })) yield break;
            yield return Capture(directory, "unowned-menu");
            if (!Check(() => Require(shell.StartNewGame(), "fresh board"))) yield break;
            yield return Settle();
            if (!Check(() => CheckButton(shell, "action-hint"))) yield break;
            yield return Capture(directory, "unowned-board");
            var before = shell.ActiveSession.Marks.ToArray();
            if (!Check(() => shell.RequestHint())) yield break;
            yield return new WaitForSecondsRealtime(0.8f);
            if (!Check(() => {
                Require(before.Count(pair => shell.ActiveSession.GetMark(pair.Key) != pair.Value) == 1, "one rewarded hint");
                Require(shell.BoardView.InteractionEnabled, "input restored after simulated dismissal");
                CheckButton(shell, "action-hint");
            })) yield break;
            yield return Capture(directory, "rewarded-hint");
            if (!Check(() => shell.ShowMenu())) yield break;
            yield return Settle();
            if (!Check(() => shell.PurchaseRemoveAds())) yield break;
            yield return Settle();
            if (!Check(() => {
                Require(!shell.Q<Button>("menu-remove-ads").enabledSelf, "verified mock owned menu");
                CheckButton(shell, "menu-remove-ads"); CheckButton(shell, "menu-restore");
            })) yield break;
            yield return Capture(directory, "owned-menu");
            if (!Check(() => shell.BackToGame())) yield break;
            yield return Settle();
            before = shell.ActiveSession.Marks.ToArray();
            if (!Check(() => {
                shell.RequestHint();
                Require(before.Count(pair => shell.ActiveSession.GetMark(pair.Key) != pair.Value) == 1, "owned hint applies directly");
                Require(shell.BoardView.InteractionEnabled, "owned input stays active");
            })) yield break;
            yield return Settle();
            if (!Check(() => CheckButton(shell, "action-hint"))) yield break;
            yield return Capture(directory, "owned-board");
            Debug.Log("MONETIZATION_REVIEW_PASSED " + Screen.width + "x" + Screen.height + " " + shell.Locale);
            Application.Quit(0);
        }
        private static IEnumerator Settle()
        { for (var i = 0; i < 12; i++) yield return new WaitForEndOfFrame(); }
        private static IEnumerator Capture(string directory, string name)
        {
            Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            yield return new WaitForSecondsRealtime(.3f);
        }
        private static void CheckButton(GameShellView shell, string name)
        {
            var button = shell.Q<Button>(name);
            Require(button != null && button.worldBound.width > 0 && button.worldBound.height >= 47, "touch target " + name);
            var measured = button.MeasureTextSize(button.text, button.contentRect.width,
                VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined);
            Require(measured.y <= button.contentRect.height + 1, "text fits " + name);
            Require(button.worldBound.xMin >= shell.worldBound.xMin - 1 && button.worldBound.xMax <= shell.worldBound.xMax + 1,
                "horizontal bounds " + name);
        }
        private static bool Check(Action check)
        { try { check(); return true; } catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; } }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Monetization mounted check: " + message); }
    }
}
#endif
