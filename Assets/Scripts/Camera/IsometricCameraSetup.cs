using UnityEngine;
using Farm2Shelf.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace Farm2Shelf.CameraSystem
{
    /// <summary>
    /// Mobil Dokunmatik Ekran & PC Çapraz Uyumlu Yüksek Performanslı İzometrik Kamera Sistemi.
    /// - Pürüzsüz & Titreşimsiz Enterpolasyon (SmoothDamp ile 60-120 FPS akıcı hareket)
    /// - Tek Parmak Sürükleme (Pan / Harita Kaydırma)
    /// - İki Parmak Sıkıştırma (Pinch-to-Zoom / Yumuşak Yakınlaştırma)
    /// - İki Parmak Dönüş (Twist / Açı Döndürme)
    /// - Fare Tekerleği & Klavye Desteği (WASD / QE / Ok Tuşları)
    /// - Yüksek Derinlik Tamponu Hassasiyeti (Z-Fighting ve yırtılma önleyici Near/Far ayarları)
    /// </summary>
    public class IsometricCameraSetup : MonoBehaviour
    {
        public static IsometricCameraSetup Instance { get; private set; }
        public Camera Cam => cam != null ? cam : (cam = GetComponent<Camera>());

        [Header("Hedef Pozisyon & Açı")]
        [SerializeField] private Vector3 targetPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private float distance = 25f;
        [SerializeField] private float pitchAngle = 50f;
        [SerializeField] private float yawAngle = 45f;
        [SerializeField] private float orthographicSize = 14f;

        [Header("Zoom Sınırları & Hız")]
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 35f;
        [SerializeField] private float scrollZoomSpeed = 6.0f;
        [SerializeField] private bool useOrthographic = true;

        [Header("Hassasiyet & Akıcılık")]
        [SerializeField] private float panSensitivity = 0.028f;
        [SerializeField] private float pinchSensitivity = 0.035f;
        [SerializeField] private float rotateSensitivity = 0.18f;
        [SerializeField] private float moveSpeed = 22f;
        [SerializeField] private float rotateSpeed = 90f;
        [SerializeField] private float smoothTime = 0.05f;
        [SerializeField] private Vector2 mapBounds = new Vector2(80f, 80f);

        // Pürüzsüz Enterpolasyon Durum Değişkenleri
        private Vector3 currentPosition;
        private Vector3 positionVelocity;

        private float currentDistance;
        private float distanceVelocity;

        private float currentYawAngle;
        private float yawVelocity;

        private float currentOrthographicSize;
        private float zoomVelocity;

        private Camera cam;
        private float lastPinchDistance;
        private float lastPinchAngle;

        private void Awake()
        {
            Instance = this;
            SetupCamera();

            // Başlangıç değerlerini eşitle
            currentPosition = targetPosition;
            currentDistance = distance;
            currentYawAngle = yawAngle;
            currentOrthographicSize = orthographicSize;
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Disable();
#endif
            if (Instance == this) Instance = null;
        }

        public void SetupCamera()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }

            cam.orthographic = useOrthographic;
            cam.orthographicSize = orthographicSize;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 180f; // Derinlik tamponu hassasiyetini 100 kat artırarak Z-fighting yırtılmalarını önler
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.26f);

            ApplyCameraTransformInstant();
        }

        private Vector2 lastMousePanPos;
        private bool isMousePanning = false;
        private bool isMousePanningAllowed = false;
        private bool isTouchOverUI = false;

        private void Update()
        {
            // Ekranda Modal/Pencere/Tablet/Duraklatma açıkken harita hareketini engelle
            bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || isPauseOpen)
            {
                isMousePanning = false;
                isMousePanningAllowed = false;
                isTouchOverUI = false;
                return;
            }

            bool isPlacing = (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing);

            HandleTouchInputs(isPlacing);
            HandleMouseScrollZoom();
            HandleMousePan(isPlacing);
            HandleKeyboardInput(isPlacing);
        }

        private void LateUpdate()
        {
            // LateUpdate: Tüm fizik ve girdi hesaplamaları bittikten sonra kamerayı pürüzsüz taşı
            SmoothUpdateCameraTransform();
        }

        private void HandleTouchInputs(bool isPlacing)
        {
            // Yerleştirme modundayken kamera jestlerini tamamen devre dışı bırak
            if (isPlacing)
            {
                isTouchOverUI = false;
                lastPinchDistance = 0f;
                lastPinchAngle = 0f;
                return;
            }

            // SADECE mobil platformlarda veya gerçek dokunmatik ekranda dokunma hareketlerini işle!
            // PC'de fare sol tıkının yanlışlıkla pinch-zoom tetiklemesini kesin olarak önler.
            if (!Application.isMobilePlatform && Input.touchCount == 0)
            {
                lastPinchDistance = 0f;
                lastPinchAngle = 0f;
                isTouchOverUI = false;
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var activeTouches = Touch.activeTouches;
                int touchCount = activeTouches.Count;
                if (touchCount == 0)
                {
                    lastPinchDistance = 0f;
                    lastPinchAngle = 0f;
                    isTouchOverUI = false;
                }
                else if (touchCount == 1)
                {
                    lastPinchDistance = 0f;
                    lastPinchAngle = 0f;

                    var touch = activeTouches[0];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        isTouchOverUI = TouchInputHelper.IsPointerOverUI(touch.screenPosition);
                    }
                    else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        isTouchOverUI = false;
                    }
                    else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && !isTouchOverUI)
                    {
                        Vector2 delta = touch.delta;
                        if (delta.sqrMagnitude > 0.01f)
                        {
                            Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
                            Vector3 moveDir = yawRot * new Vector3(-delta.x, 0f, -delta.y);

                            targetPosition += moveDir * panSensitivity * (orthographicSize / 14f);
                            ClampTargetPosition();

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraPan();
                            }
                        }
                    }
                }
                else if (touchCount >= 2)
                {
                    var touch0 = activeTouches[0];
                    var touch1 = activeTouches[1];

                    Vector2 pos0 = touch0.screenPosition;
                    Vector2 pos1 = touch1.screenPosition;

                    if (TouchInputHelper.IsPointerOverUI(pos0) || TouchInputHelper.IsPointerOverUI(pos1))
                    {
                        lastPinchDistance = 0f;
                        return;
                    }

                    float currentPinchDist = Vector2.Distance(pos0, pos1);
                    float currentPinchAngle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;

                    if (lastPinchDistance <= 0.01f || touch0.phase == UnityEngine.InputSystem.TouchPhase.Began || touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        lastPinchDistance = currentPinchDist;
                        lastPinchAngle = currentPinchAngle;
                        return;
                    }

                    if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        // 1. Zoom (Pinch) - Çift parmak pürüzsüz yakınlaştırma / uzaklaştırma
                        float deltaDist = currentPinchDist - lastPinchDistance;
                        if (Mathf.Abs(deltaDist) > 1.5f)
                        {
                            float zoomDelta = Mathf.Clamp(-deltaDist * pinchSensitivity * 0.35f * (orthographicSize / 14f), -2.5f, 2.5f);
                            orthographicSize = Mathf.Clamp(orthographicSize + zoomDelta, minZoom, maxZoom);
                            distance = Mathf.Clamp(distance + (zoomDelta * 1.5f), 6f, 60f);

                            lastPinchDistance = currentPinchDist;

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraZoom();
                            }
                        }

                        // 2. Rotate (Twist) - Çift parmakla açı döndürme
                        float deltaAngle = Mathf.DeltaAngle(lastPinchAngle, currentPinchAngle);
                        if (Mathf.Abs(deltaAngle) > 0.5f)
                        {
                            yawAngle += deltaAngle * rotateSensitivity * 1.1f;
                            lastPinchAngle = currentPinchAngle;

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraRotate();
                            }
                        }

                        // 3. Two-Finger Pan (İki parmakla harita kaydırma)
                        Vector2 delta0 = touch0.delta;
                        Vector2 delta1 = touch1.delta;
                        Vector2 avgDelta = (delta0 + delta1) * 0.5f;
                        if (avgDelta.sqrMagnitude > 0.1f)
                        {
                            Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
                            Vector3 moveDir = yawRot * new Vector3(-avgDelta.x, 0f, -avgDelta.y);
                            targetPosition += moveDir * panSensitivity * (orthographicSize / 14f);
                            ClampTargetPosition();
                        }
                    }
                }
                return;
            }
#endif

            // Legacy Touch Fallback
            try
            {
                int touchCount = Input.touchCount;
                if (touchCount == 0)
                {
                    lastPinchDistance = 0f;
                    lastPinchAngle = 0f;
                    isTouchOverUI = false;
                }
                else if (touchCount == 1)
                {
                    lastPinchDistance = 0f;
                    lastPinchAngle = 0f;

                    UnityEngine.Touch touch = Input.GetTouch(0);
                    if (touch.phase == UnityEngine.TouchPhase.Began)
                    {
                        isTouchOverUI = TouchInputHelper.IsPointerOverUI(touch.position);
                    }
                    else if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                    {
                        isTouchOverUI = false;
                    }
                    else if (touch.phase == UnityEngine.TouchPhase.Moved && !isTouchOverUI)
                    {
                        Vector2 delta = touch.deltaPosition;
                        if (delta.sqrMagnitude > 0.01f)
                        {
                            Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
                            Vector3 moveDir = yawRot * new Vector3(-delta.x, 0f, -delta.y);

                            targetPosition += moveDir * panSensitivity * (orthographicSize / 14f);
                            ClampTargetPosition();

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraPan();
                            }
                        }
                    }
                }
                else if (touchCount >= 2)
                {
                    UnityEngine.Touch touch0 = Input.GetTouch(0);
                    UnityEngine.Touch touch1 = Input.GetTouch(1);

                    Vector2 pos0 = touch0.position;
                    Vector2 pos1 = touch1.position;

                    if (TouchInputHelper.IsPointerOverUI(pos0) || TouchInputHelper.IsPointerOverUI(pos1))
                    {
                        lastPinchDistance = 0f;
                        return;
                    }

                    float currentPinchDist = Vector2.Distance(pos0, pos1);
                    float currentPinchAngle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;

                    if (lastPinchDistance <= 0.01f || touch0.phase == UnityEngine.TouchPhase.Began || touch1.phase == UnityEngine.TouchPhase.Began)
                    {
                        lastPinchDistance = currentPinchDist;
                        lastPinchAngle = currentPinchAngle;
                        return;
                    }

                    if (touch0.phase == UnityEngine.TouchPhase.Moved || touch1.phase == UnityEngine.TouchPhase.Moved)
                    {
                        // 1. Zoom (Pinch)
                        float deltaDist = currentPinchDist - lastPinchDistance;
                        if (Mathf.Abs(deltaDist) > 1.5f)
                        {
                            float zoomDelta = Mathf.Clamp(-deltaDist * pinchSensitivity * 0.35f * (orthographicSize / 14f), -2.5f, 2.5f);
                            orthographicSize = Mathf.Clamp(orthographicSize + zoomDelta, minZoom, maxZoom);
                            distance = Mathf.Clamp(distance + (zoomDelta * 1.5f), 6f, 60f);

                            lastPinchDistance = currentPinchDist;

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraZoom();
                            }
                        }

                        // 2. Rotate (Twist)
                        float deltaAngle = Mathf.DeltaAngle(lastPinchAngle, currentPinchAngle);
                        if (Mathf.Abs(deltaAngle) > 0.5f)
                        {
                            yawAngle += deltaAngle * rotateSensitivity * 1.1f;
                            lastPinchAngle = currentPinchAngle;

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraRotate();
                            }
                        }

                        // 3. Two-Finger Pan
                        Vector2 avgDelta = (touch0.deltaPosition + touch1.deltaPosition) * 0.5f;
                        if (avgDelta.sqrMagnitude > 0.1f)
                        {
                            Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
                            Vector3 moveDir = yawRot * new Vector3(-avgDelta.x, 0f, -avgDelta.y);
                            targetPosition += moveDir * panSensitivity * (orthographicSize / 14f);
                            ClampTargetPosition();
                        }
                    }
                }
            }
            catch {}
        }

        private void HandleMousePan(bool isPlacing)
        {
            if (isPlacing)
            {
                isMousePanning = false;
                isMousePanningAllowed = false;
                return;
            }

            bool isPanDown = false;
            bool wasPressedThisFrame = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isPanDown = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;
                wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame;
            }
#else
            try
            {
                isPanDown = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                wasPressedThisFrame = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            }
            catch {}
#endif

            Vector2 curMouse = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) curMouse = Mouse.current.position.ReadValue();
#else
            try { curMouse = (Vector2)Input.mousePosition; } catch {}
#endif

            if (isPanDown)
            {
                if (wasPressedThisFrame || !isMousePanning)
                {
                    if (!TouchInputHelper.IsPointerOverUI(curMouse))
                    {
                        isMousePanningAllowed = true;
                        isMousePanning = true;
                        lastMousePanPos = curMouse;
                    }
                    else
                    {
                        isMousePanningAllowed = false;
                        isMousePanning = false;
                    }
                }
                else if (isMousePanningAllowed)
                {
                    Vector2 delta = curMouse - lastMousePanPos;
                    lastMousePanPos = curMouse;

                    if (delta.sqrMagnitude > 0.001f)
                    {
                        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
                        Vector3 moveDir = yawRot * new Vector3(-delta.x, 0f, -delta.y);

                        targetPosition += moveDir * panSensitivity * (orthographicSize / 14f);
                        ClampTargetPosition();

                        if (TutorialManager.Instance != null)
                        {
                            TutorialManager.Instance.NotifyCameraPan();
                        }
                    }
                }
            }
            else
            {
                isMousePanning = false;
                isMousePanningAllowed = false;
            }
        }

        private void HandleMouseScrollZoom()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)) return;

            Vector2 curMouse = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) curMouse = Mouse.current.position.ReadValue();
#else
            try { curMouse = (Vector2)Input.mousePosition; } catch {}
#endif
            if (TouchInputHelper.IsPointerOverUI(curMouse)) return;

            float scrollY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                scrollY = Mouse.current.scroll.ReadValue().y;
            }
#endif

            if (Mathf.Abs(scrollY) < 0.001f)
            {
                try
                {
                    scrollY = Input.mouseScrollDelta.y * 120f;
                }
                catch {}
            }

            if (Mathf.Abs(scrollY) > 0.01f)
            {
                float scrollDir = Mathf.Sign(scrollY);
                float zoomDelta = Mathf.Clamp(-scrollDir * 1.5f, -2.5f, 2.5f);

                orthographicSize = Mathf.Clamp(orthographicSize + zoomDelta, minZoom, maxZoom);
                distance = Mathf.Clamp(distance + (zoomDelta * 1.4f), 5f, 60f);

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NotifyCameraZoom();
                }
            }
        }

        private void HandleKeyboardInput(bool isPlacing = false)
        {
            float dt = Time.deltaTime;
            Vector3 inputDir = Vector3.zero;
            bool qPressed = false;
            bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (!isPlacing)
                {
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDir.z += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDir.z -= 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDir.x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDir.x += 1f;
                }

                if (keyboard.qKey.isPressed) qPressed = true;
                if (keyboard.eKey.isPressed) ePressed = true;

                if (keyboard.numpadPlusKey.isPressed || keyboard.equalsKey.isPressed)
                {
                    orthographicSize = Mathf.Clamp(orthographicSize - 12f * dt, minZoom, maxZoom);
                    distance = Mathf.Clamp(distance - 18f * dt, 5f, 60f);
                }
                if (keyboard.numpadMinusKey.isPressed || keyboard.minusKey.isPressed)
                {
                    orthographicSize = Mathf.Clamp(orthographicSize + 12f * dt, minZoom, maxZoom);
                    distance = Mathf.Clamp(distance + 18f * dt, 5f, 60f);
                }
            }
#else
            try
            {
                if (!isPlacing)
                {
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDir.z += 1f;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDir.z -= 1f;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDir.x -= 1f;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDir.x += 1f;
                }

                if (Input.GetKey(KeyCode.Q)) qPressed = true;
                if (Input.GetKey(KeyCode.E)) ePressed = true;
            }
            catch (System.InvalidOperationException) {}
#endif

            if (qPressed) yawAngle -= rotateSpeed * dt;
            if (ePressed) yawAngle += rotateSpeed * dt;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir.Normalize();

                Quaternion yawRotation = Quaternion.Euler(0f, yawAngle, 0f);
                Vector3 moveVector = yawRotation * inputDir;

                targetPosition += moveVector * moveSpeed * dt * (orthographicSize / 14f);
                ClampTargetPosition();

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NotifyCameraPan();
                }
            }
        }

        private void ClampTargetPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -240f, 90f);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -140f, 195f);
        }

        private void SmoothUpdateCameraTransform()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            // Pürüzsüz Enterpolasyon (SmoothDamp)
            currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref positionVelocity, smoothTime);
            currentOrthographicSize = Mathf.SmoothDamp(currentOrthographicSize, orthographicSize, ref zoomVelocity, smoothTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, distance, ref distanceVelocity, smoothTime);
            currentYawAngle = Mathf.SmoothDampAngle(currentYawAngle, yawAngle, ref yawVelocity, smoothTime);

            cam.orthographic = useOrthographic;
            cam.orthographicSize = currentOrthographicSize;

            Quaternion rotation = Quaternion.Euler(pitchAngle, currentYawAngle, 0f);
            Vector3 position = currentPosition - (rotation * Vector3.forward * currentDistance);

            transform.position = position;
            transform.rotation = rotation;
        }

        public void ApplyCameraTransformInstant()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            currentPosition = targetPosition;
            currentOrthographicSize = orthographicSize;
            currentDistance = distance;
            currentYawAngle = yawAngle;

            cam.orthographic = useOrthographic;
            cam.orthographicSize = currentOrthographicSize;

            Quaternion rotation = Quaternion.Euler(pitchAngle, currentYawAngle, 0f);
            Vector3 position = currentPosition - (rotation * Vector3.forward * currentDistance);

            transform.position = position;
            transform.rotation = rotation;
        }

        public void FocusOn(Vector3 centerPosition, bool instant = false)
        {
            targetPosition = centerPosition;
            ClampTargetPosition();

            if (instant)
            {
                ApplyCameraTransformInstant();
            }
        }
    }
}
