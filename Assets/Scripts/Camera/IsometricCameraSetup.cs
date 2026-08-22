using UnityEngine;
using Farm2Shelf.UI;
using Farm2Shelf.Core;

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

        private void Update()
        {
            // Ekranda Modal/Pencere/Tablet açıkken veya Kutu/Mobilya taşınırken harita hareketini engelle
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen) return;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

            HandleTouchInputs();
            HandleMouseScrollZoom();
            HandleKeyboardInput();
        }

        private void LateUpdate()
        {
            // LateUpdate: Tüm fizik ve girdi hesaplamaları bittikten sonra kamerayı pürüzsüz taşı
            SmoothUpdateCameraTransform();
        }

        private void HandleTouchInputs()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#if ENABLE_INPUT_SYSTEM
            var activeTouches = Touch.activeTouches;
            int touchCount = activeTouches.Count;
            if (touchCount == 1)
            {
                var touch = activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 delta = touch.delta;
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
            else if (touchCount >= 2)
            {
                var touch0 = activeTouches[0];
                var touch1 = activeTouches[1];

                Vector2 pos0 = touch0.screenPosition;
                Vector2 pos1 = touch1.screenPosition;

                float currentPinchDist = Vector2.Distance(pos0, pos1);
                float currentPinchAngle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began || touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    lastPinchDistance = currentPinchDist;
                    lastPinchAngle = currentPinchAngle;
                }
                else if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    // 1. Zoom (Pinch)
                    float deltaDist = currentPinchDist - lastPinchDistance;
                    if (Mathf.Abs(deltaDist) > 0.5f)
                    {
                        float zoomDelta = -deltaDist * pinchSensitivity * 0.35f;
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
                    if (Mathf.Abs(deltaAngle) > 0.05f)
                    {
                        yawAngle += deltaAngle * rotateSensitivity * 1.2f;
                        lastPinchAngle = currentPinchAngle;

                        if (TutorialManager.Instance != null)
                        {
                            TutorialManager.Instance.NotifyCameraRotate();
                        }
                    }
                }
            }
#else
            try
            {
                int touchCount = Input.touchCount;
                if (touchCount == 1)
                {
                    UnityEngine.Touch touch = Input.GetTouch(0);
                    if (touch.phase == UnityEngine.TouchPhase.Moved)
                    {
                        Vector2 delta = touch.deltaPosition;
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
                else if (touchCount >= 2)
                {
                    UnityEngine.Touch touch0 = Input.GetTouch(0);
                    UnityEngine.Touch touch1 = Input.GetTouch(1);

                    Vector2 pos0 = touch0.position;
                    Vector2 pos1 = touch1.position;

                    float currentPinchDist = Vector2.Distance(pos0, pos1);
                    float currentPinchAngle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;

                    if (touch0.phase == UnityEngine.TouchPhase.Began || touch1.phase == UnityEngine.TouchPhase.Began)
                    {
                        lastPinchDistance = currentPinchDist;
                        lastPinchAngle = currentPinchAngle;
                    }
                    else if (touch0.phase == UnityEngine.TouchPhase.Moved || touch1.phase == UnityEngine.TouchPhase.Moved)
                    {
                        // 1. Zoom (Pinch)
                        float deltaDist = currentPinchDist - lastPinchDistance;
                        if (Mathf.Abs(deltaDist) > 0.5f)
                        {
                            float zoomDelta = -deltaDist * pinchSensitivity * 0.35f;
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
                        if (Mathf.Abs(deltaAngle) > 0.05f)
                        {
                            yawAngle += deltaAngle * rotateSensitivity * 1.2f;
                            lastPinchAngle = currentPinchAngle;

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyCameraRotate();
                            }
                        }
                    }
                }
            }
            catch (System.InvalidOperationException) {}
#endif
#endif
        }

        private void HandleMouseScrollZoom()
        {
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

            if (Mathf.Abs(scrollY) > 0.001f)
            {
                float scrollDir = Mathf.Sign(scrollY);
                float step = Mathf.Max(Mathf.Abs(scrollY) * 0.06f, 2.5f);
                float zoomDelta = -scrollDir * step;

                orthographicSize = Mathf.Clamp(orthographicSize + zoomDelta, minZoom, maxZoom);
                distance = Mathf.Clamp(distance + (zoomDelta * 1.4f), 5f, 60f);

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NotifyCameraZoom();
                }
            }
        }

        private void HandleKeyboardInput()
        {
            float dt = Time.deltaTime;
            Vector3 inputDir = Vector3.zero;
            bool qPressed = false;
            bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDir.z += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDir.z -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDir.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDir.x += 1f;

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
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDir.z += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDir.z -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDir.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDir.x += 1f;

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
            targetPosition.x = Mathf.Clamp(targetPosition.x, -mapBounds.x, mapBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -mapBounds.y, mapBounds.y);
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
