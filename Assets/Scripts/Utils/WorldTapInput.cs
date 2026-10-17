using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// Owns world tap gestures only. Input events are queued, then dispatched on the
    /// game thread after UI processing. No camera values or UI button callbacks are changed.
    /// </summary>
    public static class WorldTapInput
    {
        private struct Sample
        {
            public long device;
            public int kind, fingers; // 0 move, 1 down, 2 up, 3 cancel
            public bool touch;
            public Vector2 position;
            public double time;
        }
        private static readonly Queue<Sample> samples = new Queue<Sample>();
        private static bool held, rejected, ownerIsTouch;
        private static long owner = -1;
        private static Vector2 origin;
        private static double beganAt;
        private static int polledFrame = -1;
        private static bool frameTap;
        private static Vector2 framePoint;
#if ENABLE_INPUT_SYSTEM
        private static InputAction press, position;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
#if ENABLE_INPUT_SYSTEM
            press?.Dispose(); position?.Dispose(); press = position = null;
#endif
            CancelGesture(); polledFrame = -1;
        }

        public static void EnsureInitialized()
        {
#if ENABLE_INPUT_SYSTEM
            if (press != null) return;
            press = new InputAction("WorldTapPress", InputActionType.PassThrough);
            press.AddBinding("<Mouse>/leftButton");
            press.AddBinding("<Touchscreen>/touch*/press");
            position = new InputAction("WorldTapPosition", InputActionType.PassThrough);
            position.AddBinding("<Mouse>/position");
            position.AddBinding("<Touchscreen>/touch*/position");
            press.performed += ctx => Enqueue(ctx, ctx.ReadValue<float>() > .5f ? 1 : 2);
            press.canceled += ctx => Enqueue(ctx, 3);
            position.performed += ctx => Enqueue(ctx, 0);
            press.Enable(); position.Enable();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static void Enqueue(InputAction.CallbackContext ctx, int kind)
        {
            // Device Simulator and mobile focus changes can remove/reset a device
            // while an action is held. Cancellation must not read its freed state.
            if (kind == 3 || ctx.control == null || !ctx.control.device.added)
            {
                CancelGesture();
                return;
            }
            var screen = ctx.control.device as Touchscreen;
            var mouse = ctx.control.device as Mouse;
            if (screen == null && mouse == null) return;
            var contact = ctx.control.parent as UnityEngine.InputSystem.Controls.TouchControl;
            if (screen != null && contact == null) return;
            // Simulator can mirror a mouse as a touchscreen. Its screen coordinates
            // are authoritative; never combine these with the editor mouse position.
            var point = contact != null ? contact.position.ReadValue() : mouse.position.ReadValue();
            int fingers = 0;
            if (screen != null) foreach (var finger in screen.touches) if (finger.press.isPressed) fingers++;
            if (contact != null && contact.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) kind = 3;
            if (samples.Count > 256) { CancelGesture(); return; }
            long key = ((long)ctx.control.device.deviceId << 32) | (uint)(contact != null ? contact.touchId.ReadValue() : 0);
            samples.Enqueue(new Sample { device = key, touch = screen != null, fingers = fingers,
                position = point, time = ctx.time, kind = kind });
        }
#endif

        public static void CancelGesture()
        {
            samples.Clear(); held = rejected = frameTap = false; owner = -1;
        }

        public static bool Poll(out Vector2 point)
        {
            EnsureInitialized();
            if (polledFrame == Time.frameCount) { point = framePoint; return frameTap; }
            polledFrame = Time.frameCount; frameTap = false;
#if ENABLE_INPUT_SYSTEM
            bool hasTouchEvents = false;
            foreach (var sample in samples) if (sample.touch) { hasTouchEvents = true; break; }
            var screen = Touchscreen.current;
            int fingers = 0;
            if (screen != null && screen.enabled)
                foreach (var contact in screen.touches) if (contact.press.isPressed) fingers++;
            if (held && fingers > 1) rejected = true;
#else
            bool hasTouchEvents = Input.touchCount > 0;
            int fingers = Input.touchCount;
            if (fingers > 0)
            {
                var t = Input.GetTouch(0);
                int kind = t.phase == TouchPhase.Began ? 1 : t.phase == TouchPhase.Ended ? 2 : t.phase == TouchPhase.Canceled ? 3 : 0;
                samples.Enqueue(new Sample { device = t.fingerId, touch = true, position = t.position, time = Time.unscaledTimeAsDouble, kind = kind });
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || Input.GetMouseButton(0))
                samples.Enqueue(new Sample { device = -2, position = Input.mousePosition, time = Time.unscaledTimeAsDouble,
                    kind = Input.GetMouseButtonDown(0) ? 1 : Input.GetMouseButtonUp(0) ? 2 : 0 });
            if (held && fingers > 1) rejected = true;
#endif
            while (samples.Count > 0)
            {
                var sample = samples.Dequeue();
                if (hasTouchEvents && !sample.touch) continue;
                if (sample.kind == 1)
                {
                    bool secondContact = held && ownerIsTouch && sample.touch && owner != sample.device;
                    held = true; owner = sample.device; origin = sample.position; beganAt = sample.time;
                    ownerIsTouch = sample.touch;
                    rejected = secondContact || fingers > 1 || sample.fingers > 1 || WorldInteractionGate.IsBlocked(origin);
                    continue;
                }
                if (!held || sample.device != owner) continue;
                if (Vector2.Distance(origin, sample.position) > TouchInputHelper.MaxTapDragDistance) rejected = true;
                if (sample.kind == 0) continue;
                held = false; owner = -1;
                if (sample.kind == 3 || rejected || sample.time - beganAt > TouchInputHelper.MaxTapDuration) continue;
                if (!TouchInputHelper.IsValidScreenPoint(sample.position) || WorldInteractionGate.IsBlocked(sample.position)) continue;
                framePoint = sample.position; frameTap = true;
            }
            point = framePoint;
            return frameTap;
        }
    }
}
