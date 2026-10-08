using System;
using System.Collections;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #7: opt-in mounted-panel event checks, not physical-device verification.
    internal static class BoardInputReviewChecks
    {
        public static IEnumerator Run(PortraitBoardView view)
        {
            for (var frame = 0; frame < 12; frame++) yield return new WaitForEndOfFrame();
            try
            {
                foreach (var touch in new[] { false, true })
                {
                    var session = new GameSession(new PuzzleDefinition(5, 7, "input-review-fixture",
                        new[] { new BoardCoordinate(4, 4) },
                        new[] { new ArrowClue(new BoardCoordinate(2, 2), Direction.DownRight) }),
                        new[] { new BoardCoordinate(2, 3) });
                    view.Bind(session);
                    var grid = view.Q<VisualElement>("board-grid");
                    if (grid.panel == null || view.Layout.CellSize <= 0) throw new InvalidOperationException("Input panel is not mounted.");
                    var tap = new BoardCoordinate(0, 0);
                    Tap(grid, Center(view, grid, tap), touch);
                    Require(session.GetMark(tap) == PlayerMark.X, "tap Empty -> X");
                    Tap(grid, Center(view, grid, tap), touch);
                    Require(session.GetMark(tap) == PlayerMark.Cat, "tap X -> Cat");
                    Tap(grid, Center(view, grid, tap), touch);
                    Require(session.GetMark(tap) == PlayerMark.Empty, "tap Cat -> Empty");
                    var start = Center(view, grid, new BoardCoordinate(2, 0));
                    var end = Center(view, grid, new BoardCoordinate(2, 4));
                    var pointerId = touch ? PointerId.touchPointerIdBase : PointerId.mousePointerId;
                    Send(grid, start, touch, TouchPhase.Began);
                    Require(grid.HasPointerCapture(pointerId), "press captures stable grid");
                    Send(grid, Center(view, grid, new BoardCoordinate(2, 1)), touch, TouchPhase.Moved);
                    Require(grid.HasPointerCapture(pointerId), "first refresh retains capture");
                    Send(grid, end, touch, TouchPhase.Moved);
                    Require(grid.HasPointerCapture(pointerId), "sparse move and second refresh retain capture");
                    Send(grid, end, touch, TouchPhase.Ended);
                    Require(!grid.HasPointerCapture(pointerId), "release frees capture");
                    Require(session.GetMark(new BoardCoordinate(2, 0)) == PlayerMark.X, "drag origin");
                    Require(session.GetMark(new BoardCoordinate(2, 1)) == PlayerMark.X, "drag skipped sample");
                    Require(session.GetMark(new BoardCoordinate(2, 4)) == PlayerMark.X, "drag release has no extra tap");
                    Require(!session.CanInteract(new BoardCoordinate(2, 2)), "arrow remains read-only");
                    Require(!session.CanInteract(new BoardCoordinate(2, 3)), "protected remains read-only");
                    Require(view.Presentation.GetCell(new BoardCoordinate(2, 4)).Kind == BoardCellKind.X, "view refreshes");
                    var pending = Center(view, grid, new BoardCoordinate(1, 1));
                    Send(grid, pending, touch, TouchPhase.Began);
                    view.CancelInteraction();
                    Send(grid, pending, touch, TouchPhase.Ended);
                    Require(session.GetMark(new BoardCoordinate(1, 1)) == PlayerMark.Empty, "cancel suppresses release");
                    Debug.Log("BOARD_INPUT_REVIEW_PASS source=" + (touch ? "synthetic-touch" : "synthetic-mouse"));
                }
                Application.Quit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Application.Quit(1);
            }
        }
        private static Vector2 Center(PortraitBoardView view, VisualElement grid, BoardCoordinate coordinate) =>
            grid.LocalToWorld(view.Layout.GetCellRect(coordinate).center);
        private static void Require(bool condition, string assertion)
        { if (!condition) throw new InvalidOperationException("Input review failed: " + assertion); }
        private static void Tap(VisualElement grid, Vector2 position, bool touch)
        { Send(grid, position, touch, TouchPhase.Began); Send(grid, position, touch, TouchPhase.Ended); }
        private static void Send(VisualElement grid, Vector2 position, bool touch, TouchPhase phase)
        {
            var systemEvent = new Event { button = 0, mousePosition = position,
                type = phase == TouchPhase.Began ? EventType.MouseDown : phase == TouchPhase.Ended ? EventType.MouseUp : EventType.MouseDrag };
            var touchEvent = new Touch { fingerId = 0, position = position, rawPosition = position, phase = phase, tapCount = 1 };
            if (phase == TouchPhase.Began)
            {
                using (var evt = touch ? PointerDownEvent.GetPooled(touchEvent, EventModifiers.None) : PointerDownEvent.GetPooled(systemEvent))
                { evt.target = grid; grid.SendEvent(evt); }
            }
            else if (phase == TouchPhase.Ended)
            {
                using (var evt = touch ? PointerUpEvent.GetPooled(touchEvent, EventModifiers.None) : PointerUpEvent.GetPooled(systemEvent))
                { evt.target = grid; grid.SendEvent(evt); }
            }
            else
            {
                using (var evt = touch ? PointerMoveEvent.GetPooled(touchEvent, EventModifiers.None) : PointerMoveEvent.GetPooled(systemEvent))
                { evt.target = grid; grid.SendEvent(evt); }
            }
        }
    }
}
