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
    // Issue #47: explicit mounted-panel diagnostics with fake time, never enabled in normal play.
    internal static class ArrowHoldReviewChecks
    {
        public static IEnumerator Run(PortraitBoardView view, Action<double> advance, string locale, string captures)
        {
            for (var frame = 0; frame < 12; frame++) yield return new WaitForEndOfFrame();
            foreach (var touch in new[] { false, true })
            {
                var session = MakeSession();
                var origin = new BoardCoordinate(6, 0);
                var target = new BoardCoordinate(2, 4);
                var grid = view.Q<VisualElement>("board-grid");
                var pointerId = touch ? PointerId.touchPointerIdBase : PointerId.mousePointerId;
                try
                {
                    view.Bind(session, locale);
                    Require(grid.panel != null && view.Layout.CellSize > 0, "mounted geometry");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    advance(.349);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    Require(Width(view, target) == 1, "no highlight before 350 ms");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Ended);
                    Require(!grid.HasPointerCapture(pointerId), "short release frees capture");
                    view.HighlightCell(target);
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    Require(grid.HasPointerCapture(pointerId), "arrow captures stable grid");
                    if (touch)
                    {
                        Send(grid, Center(view, grid, target), true, TouchPhase.Began, 1);
                        Send(grid, Center(view, grid, target), true, TouchPhase.Ended, 1);
                        Send(grid, Center(view, grid, target), false, TouchPhase.Began);
                        Send(grid, Center(view, grid, target), false, TouchPhase.Ended);
                        Require(grid.HasPointerCapture(pointerId), "other pointer cannot finish owning hold");
                    }
                    advance(1);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    foreach (var cell in DirectionGeometry.GetDirectionalSegment(7, origin, Direction.UpRight))
                        Require(Width(view, cell) == 3, "full diagonal including marks, protection and arrow: cell=" + cell +
                            " border=" + Width(view, cell) + " capture=" + grid.HasPointerCapture(pointerId));
                    Require(Width(view, origin) == 3, "origin outlined");
                    Require(Width(view, new BoardCoordinate(6, 1)) == 1, "off-ray unchanged");
                    if (!string.IsNullOrEmpty(captures))
                    {
                        if (!Path.IsPathRooted(captures)) throw new ArgumentException("Capture directory must be absolute.");
                        Directory.CreateDirectory(captures);
                        ScreenCapture.CaptureScreenshot(Path.Combine(captures, touch ? "arrow-touch.png" : "arrow-mouse.png"));
                    }
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return new WaitForEndOfFrame();
                try
                {
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Ended);
                    Require(Width(view, origin) == 1 && Width(view, new BoardCoordinate(5, 1)) == 1, "release immediately clears path");
                    Require(Width(view, target) == 3, "release preserves live Hint outline");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began); advance(1);
                    view.HighlightCell(target, 1);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    Require(Width(view, target) == 3, "Hint expiry preserves active hold outline");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Ended);
                    Require(Width(view, target) == 1, "expired Hint is not restored after release");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    Send(grid, Center(view, grid, origin) + new Vector2(8, 0), touch, TouchPhase.Moved);
                    Require(!grid.HasPointerCapture(pointerId), "movement cancels hold");
                    Send(grid, Center(view, grid, new BoardCoordinate(6, 1)), touch, TouchPhase.Ended);
                    AssertMarks(session);
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began); advance(1);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                var captureOutDelivered = false;
                EventCallback<PointerCaptureOutEvent> captureOut = evt =>
                {
                    if (evt.pointerId == pointerId) captureOutDelivered = true;
                };
                grid.RegisterCallback(captureOut);
                try
                {
                    grid.ReleasePointer(pointerId);
                    // Unity processes pending capture changes in PointerEventBase.PostDispatch,
                    // not a scheduler update. A stationary real pointer event flushes the
                    // request without moving the gesture or fabricating capture-out.
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Moved);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                // Check the real capture-out callback and that cleanup remains stable.
                yield return PanelTurn(view);
                grid.UnregisterCallback(captureOut);
                try
                {
                    Require(captureOutDelivered, "panel delivers capture-out after release request");
                    Require(Width(view, target) == 1, "capture loss clears visible path");
                    AssertMarks(session);
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    using (var cancelled = touch
                        ? PointerCancelEvent.GetPooled(new Touch { fingerId = 0, phase = TouchPhase.Canceled }, EventModifiers.None)
                        : PointerCancelEvent.GetPooled(new Event { button = 0, type = EventType.MouseUp }))
                    { cancelled.target = grid; grid.SendEvent(cancelled); }
                    Require(!grid.HasPointerCapture(pointerId), "pointer cancellation releases capture");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    view.SetInteractionSuspended(true); advance(2);
                    Require(!grid.HasPointerCapture(pointerId), "modal/navigation suspension releases capture");
                    view.SetInteractionSuspended(false);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    Require(Width(view, target) == 1, "cancelled callback cannot activate later");
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began); advance(1);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    // A session can become completed by an external legitimate intent while held.
                    session.Tap(new BoardCoordinate(1, 5));
                    foreach (var paw in session.Puzzle.SolutionPaws) { session.Tap(paw); session.Tap(paw); }
                    Require(session.IsCompleted, "fixture completion");
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    Require(!grid.HasPointerCapture(pointerId) && Width(view, target) == 1, "completion tick restores path and capture");
                    view.Refresh();
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began); advance(2);
                    Require(!grid.HasPointerCapture(pointerId), "completed arrows remain inactive");
                    view.Bind(MakeSession(), locale);
                    Send(grid, Center(view, grid, origin), touch, TouchPhase.Began);
                    view.Bind(MakeSession(), locale); advance(2);
                }
                catch (Exception exception) { Fail(exception); yield break; }
                yield return PanelTurn(view);
                try
                {
                    Require(!grid.HasPointerCapture(pointerId) && Width(view, target) == 1, "rebind cancels pending callbacks");
                    Debug.Log("ARROW_HOLD_REVIEW_PASS source=" + (touch ? "synthetic-touch" : "synthetic-mouse") +
                        " locale=" + locale + " cellSize=" + view.Layout.CellSize);
                }
                catch (Exception exception) { Fail(exception); yield break; }
            }
            Application.Quit(0);
        }

        private static IEnumerator PanelTurn(VisualElement view)
        {
            // Frame count is not elapsed scheduler time at uncapped desktop FPS.
            // A later scheduled sentinel guarantees the 16 ms hold tick and 1 ms
            // Hint expiry were eligible to run. Fake hold time remains explicit.
            var processed = false;
            var sentinel = view.schedule.Execute(() => processed = true).StartingIn(32);
            var deadline = Time.realtimeSinceStartupAsDouble + 2;
            while (!processed && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            sentinel.Pause();
            if (!processed)
            {
                var exception = new InvalidOperationException("Arrow hold review: panel scheduler did not advance within 2 seconds.");
                Fail(exception);
                throw exception; // Stop the coroutine before any later pass/exit can run.
            }
        }

        private static GameSession MakeSession()
        {
            var session = new GameSession(new PuzzleDefinition(7, 47, "issue-47-mounted-fixture",
                new[] { new BoardCoordinate(0, 6) }, new[] {
                    new ArrowClue(new BoardCoordinate(6, 0), Direction.UpRight),
                    new ArrowClue(new BoardCoordinate(4, 2), Direction.UpRight) }), new[] { new BoardCoordinate(3, 3) });
            session.Tap(new BoardCoordinate(2, 4));
            session.Tap(new BoardCoordinate(1, 5)); session.Tap(new BoardCoordinate(1, 5));
            return session;
        }
        private static void AssertMarks(GameSession session)
        {
            Require(session.Marks.Count(pair => pair.Value == PlayerMark.X) == 1 &&
                session.GetMark(new BoardCoordinate(2, 4)) == PlayerMark.X &&
                session.Marks.Count(pair => pair.Value == PlayerMark.Paw) == 1 &&
                session.GetMark(new BoardCoordinate(1, 5)) == PlayerMark.Paw && !session.IsCompleted,
                "arrow holds never mutate marks");
        }
        private static Vector2 Center(PortraitBoardView view, VisualElement grid, BoardCoordinate coordinate) =>
            grid.LocalToWorld(view.Layout.GetCellRect(coordinate).center);
        private static float Width(PortraitBoardView view, BoardCoordinate coordinate) =>
            view.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column).style.borderLeftWidth.value;
        private static void Require(bool condition, string assertion)
        { if (!condition) throw new InvalidOperationException("Arrow hold review failed: " + assertion); }
        private static void Fail(Exception exception) { Debug.LogException(exception); Application.Quit(1); }
        private static void Send(VisualElement grid, Vector2 position, bool touch, TouchPhase phase, int finger = 0)
        {
            var mouse = new Event { button = 0, mousePosition = position,
                type = phase == TouchPhase.Began ? EventType.MouseDown : phase == TouchPhase.Ended ? EventType.MouseUp : EventType.MouseDrag };
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
    }
}
