using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// UI gestures belong to the EventSystem. The legacy fallback is used only when
    /// no native input module is available; it must never dispatch a second click.
    /// </summary>
    public static class UiReliableClick
    {
        public const int DefaultDragThreshold = 22;
        private const float ScrollDragPixels = 10f;

        private static bool pointerHeld;
        private static bool skipUntilRelease;
        private static Button parkedButton;
        private static Button.ButtonClickedEvent parkedEvent;
        private static float restoreAt = -1f;

        private static ScrollRect capturedScroll;
        private static Button capturedScrollButton;
        private static Vector2 pressScreen;
        private static Vector2 lastScreen;
        private static Vector2 contentStart;
        private static bool capturedPlacement;
        private static bool capturedSpinIcon;
        private static bool scrollDragging;
        private static float scrollVelocity;
        private static ScrollRect inertiaScroll;
        private static float inertiaVel;
        private static bool inertiaVertical = true;

        private static readonly List<RaycastResult> hits = new List<RaycastResult>(32);
        private static readonly List<Vector2> positions = new List<Vector2>(8);
#if ENABLE_INPUT_SYSTEM
        private static InputSystemUIInputModule sharedInputModule;
#endif

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        private static InputSettings editorOriginalSettings;
        private static InputSettings editorPlaySettings;

        private static void ConfigureEditorFocus()
        {
            if (!Application.isPlaying) return;
            if (editorPlaySettings == null)
            {
                editorOriginalSettings = InputSystem.settings;
                editorPlaySettings = Object.Instantiate(editorOriginalSettings);
                editorPlaySettings.name = "Farm2Shelf_EditorPlayInputSettings";
                editorPlaySettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.settings = editorPlaySettings;
                UnityEditor.EditorApplication.playModeStateChanged -= RestoreEditorInputSettings;
                UnityEditor.EditorApplication.playModeStateChanged += RestoreEditorInputSettings;
            }
            // Device Simulator may deliver touches while EventSystem reports unfocused.
            // Do not discard those events. This override never enters a mobile build.
            editorPlaySettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }

        private static void RestoreEditorInputSettings(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
            if (editorOriginalSettings != null && InputSystem.settings == editorPlaySettings)
                InputSystem.settings = editorOriginalSettings;
            if (editorPlaySettings != null) Object.DestroyImmediate(editorPlaySettings);
            editorPlaySettings = null;
            editorOriginalSettings = null;
            UnityEditor.EditorApplication.playModeStateChanged -= RestoreEditorInputSettings;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            ConfigureSharedEventSystem();
        }

        public static void ConfigureSharedEventSystem()
        {
            if (EventSystem.current == null)
            {
                var eventObject = new GameObject("EventSystem");
                eventObject.AddComponent<EventSystem>();
                Object.DontDestroyOnLoad(eventObject);
            }

            EventSystem.current.pixelDragThreshold = Mathf.Max(8, DefaultDragThreshold);

#if ENABLE_INPUT_SYSTEM
#if UNITY_EDITOR
            ConfigureEditorFocus();
#endif
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;

            StandaloneInputModule standalone = EventSystem.current.GetComponent<StandaloneInputModule>();
            if (standalone != null) standalone.enabled = false;

            InputSystemUIInputModule inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null || inputModule.point == null ||
                inputModule.point.action == null || inputModule.leftClick == null || inputModule.leftClick.action == null)
            {
                inputModule.AssignDefaultActions();
            }

            // Mouse motion must not cancel an in-progress touch (or vice versa).
            // In particular, Simulator touch positions differ from editor mouse positions.
            inputModule.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
            inputModule.enabled = true;
            sharedInputModule = inputModule;
            // An existing module can survive a scene/menu rebuild with disabled actions.
            if (!inputModule.point.action.enabled) inputModule.point.action.Enable();
            if (!inputModule.leftClick.action.enabled) inputModule.leftClick.action.Enable();
            if (inputModule.scrollWheel != null && inputModule.scrollWheel.action != null && !inputModule.scrollWheel.action.enabled)
                inputModule.scrollWheel.action.Enable();
#else
            var standalone = EventSystem.current.GetComponent<StandaloneInputModule>();
            if (standalone == null)
                standalone = EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            standalone.enabled = true;
#endif
        }

        public static void Tick()
        {
#if ENABLE_INPUT_SYSTEM
            // Recover after a scene replaces the EventSystem or disables its action map.
            // Do not run a manual click while the native module is awaiting activation.
            if (EventSystem.current == null || sharedInputModule == null ||
                sharedInputModule.gameObject != EventSystem.current.gameObject ||
                !sharedInputModule.isActiveAndEnabled || sharedInputModule.point == null ||
                sharedInputModule.point.action == null || !sharedInputModule.point.action.enabled ||
                sharedInputModule.leftClick == null || sharedInputModule.leftClick.action == null ||
                !sharedInputModule.leftClick.action.enabled)
                ConfigureSharedEventSystem();
#endif
            // EventSystem already dispatches button clicks, scrolling and placement drags.
            // A second dispatch in LateUpdate can invoke a rebuilt cart button twice and
            // overwrite the simulator's correctly transformed pointer coordinates.
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled &&
#if ENABLE_INPUT_SYSTEM
                sharedInputModule != null && sharedInputModule.isActiveAndEnabled)
#else
                EventSystem.current.currentInputModule != null &&
                EventSystem.current.currentInputModule.isActiveAndEnabled)
#endif
            {
                restoreAt = -1f;
                RestoreParkedIfDue();
                ReleaseScrollCapture(false);
                inertiaScroll = null;
                inertiaVel = 0f;
                pointerHeld = false;
                skipUntilRelease = false;
                return;
            }

            RestoreParkedIfDue();
            TickInertia();
            TickMouseWheel();

            if (EKTReklamIntroManager.Instance != null && !EKTReklamIntroManager.HasIntroFinished)
            {
                pointerHeld = TouchInputHelper.IsPointerHeld();
                skipUntilRelease = pointerHeld;
                ReleaseScrollCapture(false);
                return;
            }

            bool down = TouchInputHelper.IsPointerHeld();
            if (!down)
            {
                if (pointerHeld)
                {
                    EndPointer();
                }

                pointerHeld = false;
                skipUntilRelease = false;
                return;
            }

            if (skipUntilRelease)
            {
                return;
            }

            if (!pointerHeld)
            {
                pointerHeld = true;
                BeginPointer();
                return;
            }

            HoldPointer();
        }

        public static void IgnoreCurrentPointerUntilRelease()
        {
            skipUntilRelease = true;
            pointerHeld = true;
            capturedPlacement = false;
            capturedSpinIcon = false;
            ReleaseScrollCapture(false);
        }

        private static void BeginPointer()
        {
            if (!TryGetBestPointer(out Vector2 screenPos)) return;

            pressScreen = screenPos;
            lastScreen = screenPos;
            scrollDragging = false;
            scrollVelocity = 0f;
            capturedScroll = null;
            capturedScrollButton = null;
            capturedPlacement = false;
            capturedSpinIcon = false;
            inertiaScroll = null;

            GameObject go = FirstUiHit(screenPos);
            if (TryHandleSpinIcon(go, screenPos))
            {
                return;
            }
            if (TryHandleFurniturePlacement(go, screenPos))
            {
                return;
            }

            if (go == null)
            {
                TryClickAt(screenPos);
                return;
            }

            capturedScroll = go.GetComponentInParent<ScrollRect>();
            if (capturedScroll != null && capturedScroll.IsActive())
            {
                if (capturedScroll.content != null)
                {
                    contentStart = capturedScroll.content.anchoredPosition;
                }

                Button btn = go.GetComponentInParent<Button>();
                if (btn != null && btn.transform.IsChildOf(capturedScroll.transform))
                {
                    capturedScrollButton = btn;
                }

                return;
            }

            TryClickAt(screenPos);
        }

        private static bool TryHandleSpinIcon(GameObject go, Vector2 screenPos)
        {
            if (DailySpinWheelUI.Instance == null) return false;
            if (!DailySpinWheelUI.Instance.TryBeginIconPointer(go, screenPos)) return false;
            capturedSpinIcon = true;
            return true;
        }

        private static bool TryHandleFurniturePlacement(GameObject go, Vector2 screenPos)
        {
            FurniturePlacementManager mgr = FurniturePlacementManager.Instance;
            if (mgr == null || !mgr.IsPlacing) return false;

            if (go != null && mgr.IsPlacementHudButton(go))
            {
                TryClickAt(screenPos);
                return true;
            }

            mgr.MoveGhostToScreenPoint(screenPos);
            capturedPlacement = true;
            return true;
        }

        private static void HoldPointer()
        {
            if (!TryGetBestPointer(out Vector2 screenPos)) return;

            if (capturedPlacement)
            {
                FurniturePlacementManager mgr = FurniturePlacementManager.Instance;
                if (mgr != null && mgr.IsPlacing)
                {
                    mgr.MoveGhostToScreenPoint(screenPos);
                }
                return;
            }

            if (capturedSpinIcon)
            {
                DailySpinWheelUI.Instance?.UpdateIconPointer(screenPos);
                return;
            }

            if (capturedScroll == null || capturedScroll.content == null) return;

            Vector2 totalDelta = screenPos - pressScreen;
            if (!scrollDragging && totalDelta.magnitude >= ScrollDragPixels)
            {
                scrollDragging = true;
                capturedScroll.StopMovement();
            }

            if (!scrollDragging)
            {
                lastScreen = screenPos;
                return;
            }

            ApplyScrollByScreen(capturedScroll, lastScreen, screenPos, out float appliedY);
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            scrollVelocity = appliedY / dt;
            lastScreen = screenPos;
        }

        private static void EndPointer()
        {
            if (capturedSpinIcon)
            {
                DailySpinWheelUI.Instance?.EndIconPointer();
                capturedSpinIcon = false;
                ReleaseScrollCapture(false);
                return;
            }

            if (capturedScroll != null && scrollDragging)
            {
                inertiaScroll = capturedScroll;
                inertiaVel = scrollVelocity;
                inertiaVertical = capturedScroll.vertical;
                ReleaseScrollCapture(false);
                return;
            }

            if (capturedScroll != null && !scrollDragging && capturedScrollButton != null)
            {
                InvokeButton(capturedScrollButton);
            }

            ReleaseScrollCapture(false);
        }

        private static void ReleaseScrollCapture(bool invokePendingButton)
        {
            if (invokePendingButton && capturedScrollButton != null && !scrollDragging)
            {
                InvokeButton(capturedScrollButton);
            }

            capturedScroll = null;
            capturedScrollButton = null;
            capturedPlacement = false;
            capturedSpinIcon = false;
            scrollDragging = false;
            scrollVelocity = 0f;
        }

        private static void TickInertia()
        {
            if (inertiaScroll == null || inertiaScroll.content == null)
            {
                inertiaScroll = null;
                inertiaVel = 0f;
                return;
            }

            if (capturedScroll != null)
            {
                inertiaScroll = null;
                inertiaVel = 0f;
                return;
            }

            inertiaVel = Mathf.MoveTowards(inertiaVel, 0f, Time.unscaledDeltaTime * 2800f);
            if (Mathf.Abs(inertiaVel) < 8f)
            {
                inertiaScroll = null;
                inertiaVel = 0f;
                return;
            }

            float step = inertiaVel * Time.unscaledDeltaTime;
            ShiftContent(inertiaScroll, inertiaVertical ? new Vector2(0f, step) : new Vector2(step, 0f));
        }

        private static void TickMouseWheel()
        {
            Vector2 wheel = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                wheel = Mouse.current.scroll.ReadValue();
            }
#endif
            if (Mathf.Abs(wheel.y) < 0.01f && Mathf.Abs(wheel.x) < 0.01f) return;
            if (!TryGetBestPointer(out Vector2 screenPos)) return;

            GameObject go = FirstUiHit(screenPos);
            if (go == null) return;
            ScrollRect scroll = go.GetComponentInParent<ScrollRect>();
            if (scroll == null || !scroll.IsActive() || scroll.content == null) return;

            float step = wheel.y != 0f ? wheel.y : wheel.x;
            if (Mathf.Abs(step) > 20f) step *= 0.12f;
            ShiftContent(scroll, scroll.vertical ? new Vector2(0f, step) : new Vector2(step, 0f));
            inertiaScroll = null;
        }

        private static void ApplyScrollByScreen(ScrollRect scroll, Vector2 fromScreen, Vector2 toScreen, out float appliedAxis)
        {
            appliedAxis = 0f;
            RectTransform view = scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
            if (view == null) return;

            Camera eventCam = GetEventCamera(scroll);
            Vector2 fromLocal;
            Vector2 toLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view, fromScreen, eventCam, out fromLocal)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view, toScreen, eventCam, out toLocal)) return;

            Vector2 localDelta = toLocal - fromLocal;
            if (!scroll.horizontal) localDelta.x = 0f;
            if (!scroll.vertical) localDelta.y = 0f;
            ShiftContent(scroll, localDelta);
            appliedAxis = scroll.vertical ? localDelta.y : localDelta.x;
        }

        private static void ShiftContent(ScrollRect scroll, Vector2 localDelta)
        {
            if (scroll == null || scroll.content == null) return;
            if (localDelta.sqrMagnitude < 0.0001f) return;

            RectTransform content = scroll.content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Vector2 pos = content.anchoredPosition;
            if (scroll.horizontal) pos.x += localDelta.x;
            if (scroll.vertical) pos.y += localDelta.y;
            content.anchoredPosition = pos;

            Vector2 n = scroll.normalizedPosition;
            if (scroll.horizontal) n.x = Mathf.Clamp01(n.x);
            if (scroll.vertical) n.y = Mathf.Clamp01(n.y);
            scroll.normalizedPosition = n;
        }

        private static Camera GetEventCamera(ScrollRect scroll)
        {
            Canvas canvas = scroll.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static bool TryClickAt(Vector2 screenPos)
        {
            if (EventSystem.current == null || !TouchInputHelper.IsValidScreenPoint(screenPos))
            {
                return false;
            }

            GameObject go = FirstUiHit(screenPos);
            if (go == null) return false;

            if (go.GetComponentInParent<Slider>() != null ||
                go.GetComponentInParent<Scrollbar>() != null ||
                go.GetComponentInParent<ScrollRect>() != null)
            {
                return false;
            }

            InputField inputField = go.GetComponentInParent<InputField>();
            if (inputField != null && inputField.IsActive())
            {
                inputField.ActivateInputField();
                return true;
            }

            Toggle toggle = go.GetComponentInParent<Toggle>();
            if (toggle != null && toggle.IsActive() && toggle.IsInteractable())
            {
                toggle.isOn = !toggle.isOn;
                return true;
            }

            Button button = go.GetComponent<Button>();
            if (button == null) button = go.GetComponentInParent<Button>();
            if (button == null || !button.gameObject.activeInHierarchy) return false;
            if (go != button.gameObject && !go.transform.IsChildOf(button.transform)) return false;

            InvokeButton(button);
            return true;
        }

        private static GameObject FirstUiHit(Vector2 screenPos)
        {
            if (EventSystem.current == null) return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos,
                button = PointerEventData.InputButton.Left
            };

            hits.Clear();
            EventSystem.current.RaycastAll(eventData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                RaycastResult hit = hits[i];
                if (hit.gameObject == null) continue;
                if (hit.module is PhysicsRaycaster || hit.module is Physics2DRaycaster) continue;
                return hit.gameObject;
            }

            return null;
        }

        private static void InvokeButton(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            // Fallback only runs without an active native input module. Never replace
            // listeners or impose a global delay on unrelated buttons.
            button.onClick?.Invoke();
        }

        private static void RestoreParkedIfDue()
        {
            if (parkedButton == null && parkedEvent == null) return;
            if (parkedButton == null)
            {
                parkedEvent = null;
                restoreAt = -1f;
                return;
            }

            if (Time.unscaledTime < restoreAt) return;

            if (parkedButton != null && parkedEvent != null)
            {
                parkedButton.onClick = parkedEvent;
            }

            parkedButton = null;
            parkedEvent = null;
            restoreAt = -1f;
        }

        private static bool TryGetBestPointer(out Vector2 screenPos)
        {
            CollectPositions(positions);
            if (positions.Count > 0)
            {
                screenPos = positions[0];
                return true;
            }

            screenPos = TouchInputHelper.GetCurrentPointerPosition();
            return TouchInputHelper.IsValidScreenPoint(screenPos);
        }

        private static void CollectPositions(List<Vector2> list)
        {
            list.Clear();
            Vector2 primary = TouchInputHelper.GetCurrentPointerPosition();
            AddPosition(list, primary);

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                AddPosition(list, Touchscreen.current.primaryTouch.position.ReadValue());
            }

            if (Mouse.current != null)
            {
                AddPosition(list, Mouse.current.position.ReadValue());
            }
#endif
        }

        private static void AddPosition(List<Vector2> list, Vector2 pos)
        {
            if (!TouchInputHelper.IsValidScreenPoint(pos)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i] - pos).sqrMagnitude < 0.25f) return;
            }

            list.Add(pos);
        }
    }
}
