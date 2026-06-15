using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Dual-mode camera controller for Level Builder
    /// Topdown Mode: Fixed angle, WASD pan, scroll zoom
    /// Free Mode: Scene view style, middle mouse rotate, full freedom
    /// Press V to toggle between modes
    /// </summary>
    public class LevelBuilderCamera : MonoBehaviour
    {
        public enum CameraMode
        {
            Topdown,
            Free
        }
        
        [Header("Camera Mode")]
        [SerializeField] private CameraMode currentMode = CameraMode.Topdown;
        [SerializeField] private KeyCode toggleModeKey = KeyCode.Tab; // Tab to toggle camera mode (1/2 reserved for block types)
        
        [Header("Topdown Settings")]
        [SerializeField] private float topdownAngle = 60f;
        [SerializeField] private float topdownPanSpeed = 20f;
        [SerializeField] private float topdownFastPanSpeed = 40f;
        [SerializeField] private float topdownZoomSpeed = 10f;
        [SerializeField] private float topdownMinZoom = 5f;
        [SerializeField] private float topdownMaxZoom = 50f;
        
        [Header("Free Mode Settings")]
        [SerializeField] private float freeMoveSpeed = 10f;
        [SerializeField] private float freeFastMoveSpeed = 20f;
        [SerializeField] private float freeRotateSpeed = 3f;
        [SerializeField] private float freeZoomSpeed = 2f;
        
        [Header("Optional Features")]
        [SerializeField] private bool enableEdgePanning = false;
        [SerializeField] private float edgePanSpeed = 15f;
        [SerializeField] private int edgePanBorder = 20;
        
        [Header("Constraints")]
        [SerializeField] private bool constrainToBounds = false;
        [SerializeField] private float minX = -100f;
        [SerializeField] private float maxX = 100f;
        [SerializeField] private float minZ = -100f;
        [SerializeField] private float maxZ = 100f;
        
        // State
        private float currentZoom;
        private bool isPanning = false;
        private bool isRotating = false;
        private Vector3 lastMousePosition;
        private float pitch = 60f; // For free mode
        private float yaw = 0f;    // For free mode

        private void Start()
        {
            currentZoom = transform.position.y;
            
            // Initialize rotation
            if (currentMode == CameraMode.Topdown)
            {
                transform.rotation = Quaternion.Euler(topdownAngle, 0f, 0f);
            }
            else
            {
                // Initialize free mode angles from current rotation
                Vector3 euler = transform.rotation.eulerAngles;
                pitch = euler.x;
                yaw = euler.y;
            }
        }

        private void Update()
        {
            // Toggle camera mode
            if (Input.GetKeyDown(toggleModeKey))
            {
                ToggleMode();
            }
            
            // Update based on current mode
            if (currentMode == CameraMode.Topdown)
            {
                HandleTopdownMode();
            }
            else
            {
                HandleFreeMode();
            }
        }

        private void ToggleMode()
        {
            currentMode = currentMode == CameraMode.Topdown ? CameraMode.Free : CameraMode.Topdown;
            
            if (currentMode == CameraMode.Topdown)
            {
                // Switch to topdown - reset rotation
                transform.rotation = Quaternion.Euler(topdownAngle, 0f, 0f);
                Debug.Log("Camera Mode: TOPDOWN (Fixed angle, WASD pan, scroll zoom)");
            }
            else
            {
                // Switch to free - keep current rotation
                Vector3 euler = transform.rotation.eulerAngles;
                pitch = euler.x;
                yaw = euler.y;
                Debug.Log("Camera Mode: FREE (Scene view, middle mouse rotate, WASD move)");
            }
        }

        private void HandleTopdownMode()
        {
            HandleTopdownPanning();
            HandleTopdownZoom();
            
            // Keep rotation fixed
            transform.rotation = Quaternion.Euler(topdownAngle, 0f, 0f);
        }

        private void HandleTopdownPanning()
        {
            Vector3 moveDirection = Vector3.zero;
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? topdownFastPanSpeed : topdownPanSpeed;
            
            // WASD movement (always in world X/Z directions)
            if (Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) moveDirection += Vector3.back;
            if (Input.GetKey(KeyCode.A)) moveDirection += Vector3.left;
            if (Input.GetKey(KeyCode.D)) moveDirection += Vector3.right;
            
            // Edge panning (optional)
            if (enableEdgePanning)
            {
                if (Input.mousePosition.x >= Screen.width - edgePanBorder)
                    moveDirection += Vector3.right;
                if (Input.mousePosition.x <= edgePanBorder)
                    moveDirection += Vector3.left;
                if (Input.mousePosition.y >= Screen.height - edgePanBorder)
                    moveDirection += Vector3.forward;
                if (Input.mousePosition.y <= edgePanBorder)
                    moveDirection += Vector3.back;
            }
            
            // Middle mouse button drag
            if (Input.GetMouseButtonDown(2))
            {
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(2))
            {
                isPanning = false;
            }
            
            if (isPanning)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                moveDirection += new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * 0.01f;
                lastMousePosition = Input.mousePosition;
            }
            
            // Apply movement
            if (moveDirection.magnitude > 0)
            {
                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();
                
                Vector3 move = moveDirection * currentSpeed * Time.deltaTime;
                Vector3 newPosition = transform.position + move;
                
                // Apply constraints
                if (constrainToBounds)
                {
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
                }
                
                transform.position = newPosition;
            }
        }

        private void HandleTopdownZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (scroll != 0)
            {
                currentZoom -= scroll * topdownZoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, topdownMinZoom, topdownMaxZoom);
                
                Vector3 newPosition = transform.position;
                newPosition.y = currentZoom;
                transform.position = newPosition;
            }
        }

        private void HandleFreeMode()
        {
            HandleFreeRotation();
            HandleFreeMovement();
            HandleFreeZoom();
        }

        private void HandleFreeRotation()
        {
            // Middle mouse to rotate (like Unity Scene view)
            if (Input.GetMouseButtonDown(2))
            {
                isRotating = true;
                lastMousePosition = Input.mousePosition;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            if (Input.GetMouseButtonUp(2))
            {
                isRotating = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            
            if (isRotating)
            {
                float mouseX = Input.GetAxis("Mouse X") * freeRotateSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * freeRotateSpeed;
                
                yaw += mouseX;
                pitch -= mouseY;
                pitch = Mathf.Clamp(pitch, -89f, 89f); // Prevent gimbal lock
                
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        private void HandleFreeMovement()
        {
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? freeFastMoveSpeed : freeMoveSpeed;
            Vector3 moveDirection = Vector3.zero;
            
            // WASD movement relative to camera direction
            if (Input.GetKey(KeyCode.W)) moveDirection += transform.forward;
            if (Input.GetKey(KeyCode.S)) moveDirection += -transform.forward;
            if (Input.GetKey(KeyCode.D)) moveDirection += transform.right;
            if (Input.GetKey(KeyCode.A)) moveDirection += -transform.right;
            
            // Space/Ctrl for up/down
            if (Input.GetKey(KeyCode.Space)) moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                moveDirection += Vector3.down;
            
            if (moveDirection.magnitude > 0)
            {
                moveDirection.Normalize();
                Vector3 newPosition = transform.position + moveDirection * currentSpeed * Time.deltaTime;
                
                if (constrainToBounds)
                {
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
                }
                
                transform.position = newPosition;
            }
        }

        private void HandleFreeZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (scroll != 0)
            {
                // Move camera forward/backward in its view direction
                Vector3 moveDir = transform.forward * scroll * freeZoomSpeed;
                transform.position += moveDir;
            }
        }

        // Help text now lives in the Canvas UI (LevelBuilderUI's help panel,
        // toggled with F1). The old IMGUI overlay is removed to avoid clashing
        // with the redesigned Canvas UI.
    }
}
