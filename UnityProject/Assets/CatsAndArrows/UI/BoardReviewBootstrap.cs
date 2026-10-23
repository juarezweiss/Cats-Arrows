#if !PAWS_PRODUCTION
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Explicit review-scene entry point, never a global runtime initializer.
    [RequireComponent(typeof(UIDocument))]
    public sealed class BoardReviewBootstrap : MonoBehaviour
    {
        [SerializeField] private int seed = 14;
        [SerializeField] private bool inlineHints = true;
        [SerializeField] private bool tutorialEnabled = true;
        private PortraitBoardView view;
        private GameShellView shell;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private ReviewAdService reviewAds;
#endif
        private float safeTop;
        private float safeBottom;
        private bool simulateSafeArea;
        private bool applicationFocused = true;
        private bool applicationPaused;
        private void Start()
        {
            var document = GetComponent<UIDocument>();
            var root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.style.flexGrow = 1;
            #if CATS_ARROWS_GAME_LOOP_CHECK
            const bool autoReview = true;
#else
            const bool autoReview = false;
#endif
            var locale = Argument("--board-locale=") ?? CultureInfo.CurrentUICulture.Name;
            var capture = Argument("--board-capture=");
            var menuCapture = Argument("--menu-capture=");
            var settingsCapture = Argument("--settings-capture=");
            var languageCapture = Argument("--language-capture=");
            var settingsReview = !string.IsNullOrEmpty(settingsCapture) || !string.IsNullOrEmpty(languageCapture);
            var tutorialCheck = Argument("--tutorial-check=") == "1";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var monetizationCheck = Argument("--monetization-check=") == "1";
            if (monetizationCheck) Application.runInBackground = true;
#endif
            var arrowHoldCheck = Argument("--arrow-hold-check=") == "1";
            var hintIdleCheck = Argument("--hint-idle-check=") == "1";
            var completionCheck = Argument("--completion-check=") == "1";
            double holdReviewTime = 0;
            if (settingsReview || completionCheck || hintIdleCheck || arrowHoldCheck || tutorialCheck || autoReview || !string.IsNullOrEmpty(capture) || !string.IsNullOrEmpty(menuCapture) || Argument("--board-input-check=") == "1" || Argument("--game-loop-check=") == "1") Application.runInBackground = true;
            // Opt-in capture insets are pixels, like Screen.safeArea; real devices use their reported safe area.
            simulateSafeArea = float.TryParse(Argument("--board-safe-top="), NumberStyles.Float, CultureInfo.InvariantCulture, out safeTop) |
                float.TryParse(Argument("--board-safe-bottom="), NumberStyles.Float, CultureInfo.InvariantCulture, out safeBottom);
            safeTop = float.IsFinite(safeTop) ? Mathf.Max(0, safeTop) : 0;
            safeBottom = float.IsFinite(safeBottom) ? Mathf.Max(0, safeBottom) : 0;
            if (int.TryParse(Argument("--board-width="), out var width) &&
                int.TryParse(Argument("--board-height="), out var height) && width > 0 && height > 0)
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
            if (Argument("--board-gallery=") == "1") root.Add(new BoardSymbolGalleryView(locale));
            else if (arrowHoldCheck || !string.IsNullOrEmpty(capture) || Argument("--board-input-check=") == "1")
            {
                view = new PortraitBoardView(arrowHoldCheck ? (Func<double>)(() => holdReviewTime) : null); root.Add(view);
                var result = new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, seed);
                if (result.IsSuccess) view.Bind(new GameSession(result.Puzzle), locale);
                else view.ShowGenerationFailure(result.Status, locale);
            }
            else
            {
                var initialSeed = autoReview ? seed : int.TryParse(Argument("--game-seed="), out var reviewSeed)
                    ? reviewSeed : unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount);
#if CATS_ARROWS_LEGACY_HINTS
                const bool useInlineHints = false;
#else
                var useInlineHints = Argument("--hint-flow=") == "legacy" ? false : inlineHints;
#endif
                var reviewSeen = false;
                MonetizationCoordinator monetization = null;
                IAdPrivacyService adPrivacy = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Argument("--admob-privacy-mock=") == "1") adPrivacy = new ReviewAdPrivacyService();
                if (Argument("--monetization-mock=") == "1")
                {
                    var reviewDirectory = Path.Combine(Application.persistentDataPath, "monetization-mock-review");
                    if (monetizationCheck)
                    {
                        var captures = Argument("--game-captures=");
                        if (string.IsNullOrEmpty(captures) || !Path.IsPathRooted(captures))
                            throw new ArgumentException("Monetization check captures must be absolute.");
                        // Each mounted check is isolated from manually reviewed ownership.
                        reviewDirectory = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
                    }
                    reviewAds = new ReviewAdService(() => Time.realtimeSinceStartupAsDouble);
                    monetization = new MonetizationCoordinator(true,
                        new FileMonetizationStateStore(Path.Combine(reviewDirectory, "state.bin")), reviewAds,
                        new ReviewPurchaseService(Path.Combine(reviewDirectory, "mock-store.txt")),
                        () => Time.realtimeSinceStartupAsDouble);
                }
#endif
                var isolatedReview = settingsReview || completionCheck || hintIdleCheck || tutorialCheck || autoReview || !string.IsNullOrEmpty(menuCapture) || Argument("--game-loop-check=") == "1";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                isolatedReview |= monetizationCheck;
#endif
                shell = new GameShellView(initialSeed, autoReview ? "en" : Argument("--board-locale="), useInlineHints: useInlineHints,
                    enableTutorial: hintIdleCheck || tutorialCheck || (tutorialEnabled && Argument("--tutorial=") != "off" && !autoReview && Argument("--game-loop-check=") != "1"),
                    tutorialSeen: isolatedReview ? (Func<bool>)(() => reviewSeen) : null,
                    markTutorialSeen: isolatedReview ? (Action)(() => reviewSeen = true) : null,
                    difficultyHistory: isolatedReview ? new MemoryDifficultyHistoryStore() : null,
                    idleClock: hintIdleCheck ? (Func<double>)(() => holdReviewTime) : null, monetization: monetization,
                    completionClock: completionCheck ? (Func<double>)(() => holdReviewTime) : null, adPrivacy: adPrivacy);
                root.Add(shell);
                shell.SetApplicationFocus(applicationFocused);
                shell.SetApplicationPaused(applicationPaused);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (monetizationCheck && shell != null)
                StartCoroutine(MonetizationReviewChecks.Run(shell, Argument("--game-captures=")));
            else
#endif
            if (completionCheck && shell != null) StartCoroutine(CompletionReviewChecks.Run(shell, seconds => holdReviewTime += seconds, Argument("--game-captures=")));
            else if (hintIdleCheck && shell != null) StartCoroutine(HintIdleReviewChecks.Run(shell, seconds => holdReviewTime += seconds, locale, Argument("--game-captures=")));
            else if (arrowHoldCheck && view != null) StartCoroutine(ArrowHoldReviewChecks.Run(view, seconds => holdReviewTime += seconds, locale, Argument("--game-captures=")));
            else if (tutorialCheck && shell != null) StartCoroutine(TutorialReviewChecks.Run(shell, Argument("--game-captures=")));
            else if ((autoReview || Argument("--game-loop-check=") == "1") && shell != null) StartCoroutine(GameLoopReviewChecks.Run(shell, Argument("--game-captures=")));
            else if (Argument("--board-input-check=") == "1" && view != null) StartCoroutine(BoardInputReviewChecks.Run(view));
            else if (!string.IsNullOrEmpty(capture)) StartCoroutine(Capture(capture));
            else if (settingsReview && shell != null)
            {
                shell.ShowSettings();
                if (!string.IsNullOrEmpty(languageCapture)) shell.ShowLanguageSelector();
                StartCoroutine(Capture(languageCapture ?? settingsCapture));
            }
            else if (!string.IsNullOrEmpty(menuCapture)) StartCoroutine(Capture(menuCapture));
        }
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            reviewAds?.Tick();
#endif
            shell?.TickMonetization();
            if (shell != null)
            {
                if (simulateSafeArea) shell.ApplySafeArea(new Rect(0, safeBottom, Screen.width, Mathf.Max(0, Screen.height - safeTop - safeBottom)), new Vector2(Screen.width, Screen.height));
                else shell.UpdateSafeArea();
            }
            if (view == null) return;
            if (simulateSafeArea)
                view.ApplySafeArea(new Rect(0, safeBottom, Screen.width, Mathf.Max(0, Screen.height - safeTop - safeBottom)),
                    new Vector2(Screen.width, Screen.height));
            else view.UpdateSafeArea();
        }
        private void OnApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            if (!focused) view?.CancelInteraction();
            shell?.SetApplicationFocus(focused);
            if (!focused) shell?.SuspendMonetizationDelivery();
            else shell?.RefreshMonetization();
        }
        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) view?.CancelInteraction();
            shell?.SetApplicationPaused(paused);
            if (paused) shell?.SuspendMonetizationDelivery();
            else shell?.RefreshMonetization();
        }
        private void OnDestroy() => shell?.DisposeMonetization();
        private IEnumerator Capture(string path)
        {
            if (!Path.IsPathRooted(path)) throw new ArgumentException("Review capture path must be absolute.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Let the panel resolve geometry and load its built-in font before capturing real pixels.
            for (var frame = 0; frame < 12; frame++) yield return new WaitForEndOfFrame();
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root.panel == null || !float.IsFinite(root.contentRect.width) || !float.IsFinite(root.contentRect.height) ||
                root.contentRect.width <= 0 || root.contentRect.height <= 0 ||
                (view?.Presentation != null && view.Layout.CellSize <= 0))
            {
                Debug.LogError("Board review panel did not mount with valid geometry.");
                Application.Quit(1); yield break;
            }
            Debug.Log("BOARD_REVIEW_GEOMETRY root=" + root.contentRect + " view=" + (view == null ? "gallery" : view.contentRect.ToString()) +
                " boardSize=" + (view?.Presentation?.Size ?? 0) + " cellSize=" + (view?.Layout.CellSize ?? 0));
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(1);
            if (File.Exists(path)) { Debug.Log("BOARD_REVIEW_CAPTURE " + path); Application.Quit(0); }
            else { Debug.LogError("Board review screenshot was not written."); Application.Quit(1); }
        }
        private static string Argument(string prefix)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
                if (argument.StartsWith(prefix, StringComparison.Ordinal)) return argument.Substring(prefix.Length);
            return null;
        }
    }
}

#endif
