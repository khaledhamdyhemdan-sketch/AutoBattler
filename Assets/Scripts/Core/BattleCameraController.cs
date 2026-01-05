using UnityEngine;
using Unity.Mathematics;

namespace AutoBattler.Core
{
    /// <summary>
    /// Unity 6.3 camera controller for Auto-Battler with orbit, zoom, and pan controls
    /// Supports smooth transitions and auto-framing during combat
    /// </summary>
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform focusPoint;
        [SerializeField] private Vector3 defaultFocusPosition = new Vector3(0, 0, 0);
        [SerializeField] private bool autoFocusBattlefield = true;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitSensitivity = 100f;
        [SerializeField] private float minVerticalAngle = 10f;
        [SerializeField] private float maxVerticalAngle = 80f;
        [SerializeField] private float defaultDistance = 15f;
        [SerializeField] private float defaultHorizontalAngle = 0f;
        [SerializeField] private float defaultVerticalAngle = 45f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSensitivity = 2f;
        [SerializeField] private float minZoomDistance = 5f;
        [SerializeField] private float maxZoomDistance = 30f;
        [SerializeField] private float zoomDamping = 5f;

        [Header("Pan Settings")]
        [SerializeField] private float panSensitivity = 0.5f;
        [SerializeField] private float panDamping = 5f;
        [SerializeField] private Vector2 panBounds = new Vector2(15f, 15f);

        [Header("Smoothing")]
        [SerializeField] private float rotationDamping = 5f;
        [SerializeField] private float movementDamping = 5f;
        [SerializeField] private bool useSmoothing = true;

        [Header("Input Settings")]
        [SerializeField] private bool enableOrbit = true;
        [SerializeField] private bool enableZoom = true;
        [SerializeField] private bool enablePan = true;
        [SerializeField] private bool invertVertical = false;

        [Header("Auto-Focus Settings")]
        [SerializeField] private bool autoFocusDuringCombat = true;
        [SerializeField] private float autoFocusSpeed = 2f;
        [SerializeField] private float autoFocusPadding = 2f;

        // Runtime state
        private float currentDistance;
        private float targetDistance;
        private float horizontalAngle;
        private float verticalAngle;
        private Vector3 currentFocusPosition;
        private Vector3 targetFocusPosition;
        private Vector3 panOffset;
        private bool isDragging = false;
        private bool isPanning = false;
        private Vector3 lastMousePosition;

        // References
        private Camera cam;
        private GameManager gameManager;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("CameraController requires a Camera component!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Auto-find focus point if not assigned
            if (focusPoint == null)
            {
                GameObject focusObj = GameObject.Find("CameraFocusPoint");
                if (focusObj != null)
                    focusPoint = focusObj.transform;
            }

            // Auto-find game manager
            if (gameManager == null)
                gameManager = GameManager.Instance;

            // Initialize values
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;
            horizontalAngle = defaultHorizontalAngle;
            verticalAngle = defaultVerticalAngle;
            
            currentFocusPosition = focusPoint != null ? focusPoint.position : defaultFocusPosition;
            targetFocusPosition = currentFocusPosition;
            panOffset = Vector3.zero;

            // Get GameManager reference
            gameManager = GameManager.Instance;

            // Subscribe to phase changes for auto-focus
            if (gameManager != null && autoFocusDuringCombat)
            {
                gameManager.OnPhaseChanged += HandlePhaseChanged;
            }

            // Set initial position
            UpdateCameraPosition(true);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraPosition(false);
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current == null) return;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            
            // Orbit control (right mouse button)
            if (enableOrbit)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    isDragging = true;
                    lastMousePosition = mouse.position.ReadValue();
                }
                else if (mouse.rightButton.wasReleasedThisFrame)
                {
                    isDragging = false;
                }

                if (isDragging)
                {
                    Vector3 currentPos = mouse.position.ReadValue();
                    Vector3 delta = currentPos - lastMousePosition;
                    lastMousePosition = currentPos;

                    horizontalAngle += delta.x * orbitSensitivity * Time.deltaTime;
                    verticalAngle += delta.y * orbitSensitivity * Time.deltaTime * (invertVertical ? -1f : 1f);
                    verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
                }
            }

            // Zoom control (mouse wheel)
            if (enableZoom)
            {
                float scrollDelta = mouse.scroll.ReadValue().y / 120f;
                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    targetDistance -= scrollDelta * zoomSensitivity;
                    targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
                }
            }

            // Pan control (middle mouse button)
            if (enablePan)
            {
                if (mouse.middleButton.wasPressedThisFrame)
                {
                    isPanning = true;
                    lastMousePosition = mouse.position.ReadValue();
                }
                else if (mouse.middleButton.wasReleasedThisFrame)
                {
                    isPanning = false;
                }

                if (isPanning)
                {
                    Vector3 currentPos = mouse.position.ReadValue();
                    Vector3 delta = currentPos - lastMousePosition;
                    lastMousePosition = currentPos;

                    // Convert screen space delta to world space
                    Vector3 right = transform.right;
                    Vector3 up = Vector3.up;

                    Vector3 panDelta = (-right * delta.x - up * delta.y) * panSensitivity * Time.deltaTime;
                    panOffset += panDelta;

                    // Clamp pan within bounds
                    panOffset.x = Mathf.Clamp(panOffset.x, -panBounds.x, panBounds.x);
                    panOffset.z = Mathf.Clamp(panOffset.z, -panBounds.y, panBounds.y);
                }
            }

            // Reset camera (R key)
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetCamera();
            }
#else
            // Use UnityEngine.Input which works with both input systems when "Both" is selected
            // or with legacy input when that's active
            
            // Orbit control (right mouse button)
            if (enableOrbit)
            {
                if (UnityEngine.Input.GetMouseButtonDown(1))
                {
                    isDragging = true;
                    lastMousePosition = UnityEngine.Input.mousePosition;
                }
                else if (UnityEngine.Input.GetMouseButtonUp(1))
                {
                    isDragging = false;
                }

                if (isDragging)
                {
                    Vector3 delta = UnityEngine.Input.mousePosition - lastMousePosition;
                    lastMousePosition = UnityEngine.Input.mousePosition;

                    horizontalAngle += delta.x * orbitSensitivity * Time.deltaTime;
                    verticalAngle += delta.y * orbitSensitivity * Time.deltaTime * (invertVertical ? -1f : 1f);
                    verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
                }
            }

            // Zoom control (mouse wheel)
            if (enableZoom)
            {
                float scrollDelta = UnityEngine.Input.mouseScrollDelta.y;
                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    targetDistance -= scrollDelta * zoomSensitivity;
                    targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
                }
            }

            // Pan control (middle mouse button)
            if (enablePan)
            {
                if (UnityEngine.Input.GetMouseButtonDown(2))
                {
                    isPanning = true;
                    lastMousePosition = UnityEngine.Input.mousePosition;
                }
                else if (UnityEngine.Input.GetMouseButtonUp(2))
                {
                    isPanning = false;
                }

                if (isPanning)
                {
                    Vector3 delta = UnityEngine.Input.mousePosition - lastMousePosition;
                    lastMousePosition = UnityEngine.Input.mousePosition;

                    // Convert screen space delta to world space
                    Vector3 right = transform.right;
                    Vector3 up = Vector3.up;

                    Vector3 panDelta = (-right * delta.x - up * delta.y) * panSensitivity * Time.deltaTime;
                    panOffset += panDelta;

                    // Clamp pan within bounds
                    panOffset.x = Mathf.Clamp(panOffset.x, -panBounds.x, panBounds.x);
                    panOffset.z = Mathf.Clamp(panOffset.z, -panBounds.y, panBounds.y);
                }
            }

            // Reset camera (R key)
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                ResetCamera();
            }
#endif
        }

        private void UpdateCameraPosition(bool immediate)
        {
            // Auto-focus on battlefield during combat
            if (autoFocusBattlefield && autoFocusDuringCombat && gameManager != null)
            {
                if (gameManager.CurrentPhase == GamePhase.Combat)
                {
                    targetFocusPosition = CalculateBattlefieldCenter();
                }
                else
                {
                    targetFocusPosition = focusPoint != null ? focusPoint.position : defaultFocusPosition;
                }
            }
            else
            {
                targetFocusPosition = focusPoint != null ? focusPoint.position : defaultFocusPosition;
            }

            // Smooth transitions
            if (useSmoothing && !immediate)
            {
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomDamping * Time.deltaTime);
                currentFocusPosition = Vector3.Lerp(currentFocusPosition, targetFocusPosition + panOffset, movementDamping * Time.deltaTime);
            }
            else
            {
                currentDistance = targetDistance;
                currentFocusPosition = targetFocusPosition + panOffset;
            }

            // Calculate camera position using spherical coordinates
            float horizontalRad = horizontalAngle * Mathf.Deg2Rad;
            float verticalRad = verticalAngle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad),
                Mathf.Sin(verticalRad),
                Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad)
            ) * currentDistance;

            transform.position = currentFocusPosition + offset;
            transform.LookAt(currentFocusPosition);
        }

        private Vector3 CalculateBattlefieldCenter()
        {
            // Find all active units
            var combatManager = CombatManager.Instance;
            if (combatManager == null) return defaultFocusPosition;

            Vector3 center = Vector3.zero;
            int count = 0;

            // Get all player units
            var playerUnits = GameObject.FindGameObjectsWithTag("Player");
            foreach (var unitObj in playerUnits)
            {
                var unit = unitObj.GetComponent<UnitController>();
                if (unit != null && !unit.IsDead)
                {
                    center += unit.transform.position;
                    count++;
                }
            }

            // Get all enemy units
            var enemyUnits = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var unitObj in enemyUnits)
            {
                var unit = unitObj.GetComponent<UnitController>();
                if (unit != null && !unit.IsDead)
                {
                    center += unit.transform.position;
                    count++;
                }
            }

            if (count > 0)
            {
                return center / count;
            }

            return defaultFocusPosition;
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Combat)
            {
                // Optionally zoom in slightly for combat
                targetDistance = defaultDistance * 0.8f;
            }
            else if (newPhase == GamePhase.Preparation)
            {
                // Reset to default view for preparation
                targetDistance = defaultDistance;
                panOffset = Vector3.zero;
            }
        }

        /// <summary>
        /// Reset camera to default position and rotation
        /// </summary>
        public void ResetCamera()
        {
            targetDistance = defaultDistance;
            horizontalAngle = defaultHorizontalAngle;
            verticalAngle = defaultVerticalAngle;
            panOffset = Vector3.zero;
            targetFocusPosition = focusPoint != null ? focusPoint.position : defaultFocusPosition;
        }

        /// <summary>
        /// Focus camera on a specific position
        /// </summary>
        public void FocusOnPosition(Vector3 position, float distance = -1f)
        {
            targetFocusPosition = position;
            panOffset = Vector3.zero;
            if (distance > 0)
            {
                targetDistance = Mathf.Clamp(distance, minZoomDistance, maxZoomDistance);
            }
        }

        /// <summary>
        /// Focus camera on a specific transform
        /// </summary>
        public void FocusOnTarget(Transform target, float distance = -1f)
        {
            if (target != null)
            {
                FocusOnPosition(target.position, distance);
            }
        }

        /// <summary>
        /// Set camera angle (horizontal, vertical in degrees)
        /// </summary>
        public void SetAngle(float horizontal, float vertical)
        {
            horizontalAngle = horizontal;
            verticalAngle = Mathf.Clamp(vertical, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// Enable/disable camera controls
        /// </summary>
        public void SetControlsEnabled(bool orbit, bool zoom, bool pan)
        {
            enableOrbit = orbit;
            enableZoom = zoom;
            enablePan = pan;
        }

        // Properties for external access
        public float CurrentDistance => currentDistance;
        public Vector3 FocusPosition => currentFocusPosition;
        public bool IsDragging => isDragging;
        public bool IsPanning => isPanning;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw focus point
            Vector3 focus = focusPoint != null ? focusPoint.position : defaultFocusPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(focus + panOffset, 0.5f);

            // Draw pan bounds
            Gizmos.color = Color.cyan;
            Vector3 boundsCenter = focus;
            Gizmos.DrawWireCube(boundsCenter, new Vector3(panBounds.x * 2, 0.1f, panBounds.y * 2));
        }
#endif
    }
}
