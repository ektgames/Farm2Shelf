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
    /// Mobil Dokunmatik Ekran Uyumlu İzometrik Kamera Sistemi.
    /// - Tek Parmak Sürükleme (Pan / Harita Kaydırma)
    /// - İki Parmak Sıkıştırma (Pinch-to-Zoom / Yakınlaştırma)
    /// - İki Parmak Dönüş (Twist / Kamera Açı Döndürme)
    /// - Klavye WASD / QE Editör Desteği
    /// Modal pencere açıkken veya mobilya yerleştirilirken girdileri otomatik durdurur.
    /// </summary>
    public class IsometricCameraSetup : MonoBehaviour
    {
        [Header("Kamera Kurulum Ayarları")]
        [SerializeField] private Vector3 targetPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private float distance = 25f;
        [SerializeField] private float pitchAngle = 50f;
        [SerializeField] private float yawAngle = 45f;
        [SerializeField] private float orthographicSize = 14f;
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 35f;
        [SerializeField] private float scrollZoomSpeed = 7.0f;
        [SerializeField] private bool useOrthographic = true;

        [Header("Dokunmatik & Klavye Ayarları")]
        [SerializeField] private float panSensitivity = 0.035f;
        [SerializeField] private float pinchSensitivity = 0.045f;
        [SerializeField] private float rotateSensitivity = 0.2f;
        [SerializeField] private float moveSpeed = 18f;
        [SerializeField] private float rotateSpeed = 100f;
        [SerializeField] private Vector2 mapBounds = new Vector2(80f, 80f);

        private Camera cam;
        private Vector2 lastTouchPos;
        private float lastPinchDistance;
        private float lastPinchAngle;

        private void Awake()
        {
            SetupCamera();
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
        }

        private void Update()
        {
            // Ekranda herhangi bir Modal/Pencere açıkken veya Kutu/Mobilya taşınırken arka planda kamera hareketini engelle!
            if (ModalManager.IsModalOpen) return;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

            HandleTouchInputs();
            HandleMouseScrollZoom();
            HandleKeyboardInput();
            UpdateCameraTransform();
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.25f, 0.28f);

            UpdateCameraTransform();
        }

        private void HandleTouchInputs()
        {
            // Bilgisayarda / Editörde Fare Sol Tıkının kamerayı kaydırmasını veya zoom yapmasını kesin olarak engeller!
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

                    targetPosition += moveDir * panSensitivity * (cam.orthographicSize / 14f);
                    ClampTargetPosition();
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

                        if (cam != null) cam.orthographicSize = orthographicSize;
                        lastPinchDistance = currentPinchDist;
                    }

                    // 2. Rotate (Twist / İki Parmak Kamerayı Döndürme)
                    float deltaAngle = Mathf.DeltaAngle(lastPinchAngle, currentPinchAngle);
                    if (Mathf.Abs(deltaAngle) > 0.05f)
                    {
                        yawAngle += deltaAngle * rotateSensitivity * 1.2f;
                        lastPinchAngle = currentPinchAngle;
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

                        targetPosition += moveDir * panSensitivity * (cam.orthographicSize / 14f);
                        ClampTargetPosition();
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

                            if (cam != null) cam.orthographicSize = orthographicSize;
                            lastPinchDistance = currentPinchDist;
                        }

                        // 2. Rotate (Twist / İki Parmak Kamerayı Döndürme)
                        float deltaAngle = Mathf.DeltaAngle(lastPinchAngle, currentPinchAngle);
                        if (Mathf.Abs(deltaAngle) > 0.05f)
                        {
                            yawAngle += deltaAngle * rotateSensitivity * 1.2f;
                            lastPinchAngle = currentPinchAngle;
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
                float step = Mathf.Max(Mathf.Abs(scrollY) * 0.08f, 3.0f);
                float zoomDelta = -scrollDir * step;

                orthographicSize = Mathf.Clamp(orthographicSize + zoomDelta, minZoom, maxZoom);
                distance = Mathf.Clamp(distance + (zoomDelta * 1.5f), 5f, 60f);

                if (cam == null) cam = GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = useOrthographic;
                    cam.orthographicSize = orthographicSize;
                }

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
                if (keyboard.wKey.isPressed) inputDir.z += 1f;
                if (keyboard.sKey.isPressed) inputDir.z -= 1f;
                if (keyboard.aKey.isPressed) inputDir.x -= 1f;
                if (keyboard.dKey.isPressed) inputDir.x += 1f;

                if (keyboard.qKey.isPressed) qPressed = true;
                if (keyboard.eKey.isPressed) ePressed = true;

                // Klavye Numpad + / - veya Eşittir / Eksi tuşları ile Zoom desteği
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
                if (Input.GetKey(KeyCode.W)) inputDir.z += 1f;
                if (Input.GetKey(KeyCode.S)) inputDir.z -= 1f;
                if (Input.GetKey(KeyCode.A)) inputDir.x -= 1f;
                if (Input.GetKey(KeyCode.D)) inputDir.x += 1f;

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

                targetPosition += moveVector * moveSpeed * dt;
                ClampTargetPosition();
            }
        }

        private void ClampTargetPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -mapBounds.x, mapBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -mapBounds.y, mapBounds.y);
        }

        private void UpdateCameraTransform()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            cam.orthographic = useOrthographic;
            cam.orthographicSize = orthographicSize;

            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 position = targetPosition - (rotation * Vector3.forward * distance);

            transform.position = position;
            transform.rotation = rotation;
        }

        public void FocusOn(Vector3 centerPosition)
        {
            targetPosition = centerPosition;
            UpdateCameraTransform();
        }
    }
}
