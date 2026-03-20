using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneAtlas.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;

        [Header("Room Bounds")]
        public Vector2 roomMin = new Vector2(-8f, -4.5f);
        public Vector2 roomMax = new Vector2(8f, 4.5f);
        public Vector2 roomCenter = Vector2.zero;

        [Header("Door Config")]
        public float doorWidth = 0.75f; // How wide the door detection zone is (in units from center)

        [Header("Sprites")]
        public SpriteRenderer spriteRenderer;

        private InputAction moveAction;
        private Vector2 moveInput;
        private Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Small box at feet for natural collision
            var col = GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.15f, 0.1f);
            col.offset = new Vector2(0f, 0.05f);
        }

        void OnEnable()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            moveAction?.Dispose();
        }

        void FixedUpdate()
        {
            moveInput = moveAction.ReadValue<Vector2>();

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector2 targetPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

                // No clamping — collision walls handle room boundaries.
                // Only add a generous outer clamp to prevent escaping entirely.
                float safeMargin = 1f;
                targetPos.x = Mathf.Clamp(targetPos.x,
                    roomCenter.x + roomMin.x - safeMargin,
                    roomCenter.x + roomMax.x + safeMargin);
                targetPos.y = Mathf.Clamp(targetPos.y,
                    roomCenter.y + roomMin.y - safeMargin,
                    roomCenter.y + roomMax.y + safeMargin);

                rb.MovePosition(targetPos);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        void Update()
        {
            if (moveInput.x < -0.1f && spriteRenderer != null)
                spriteRenderer.flipX = true;
            else if (moveInput.x > 0.1f && spriteRenderer != null)
                spriteRenderer.flipX = false;
        }

        /// <summary>
        /// Checks if the player is at an exit. Only triggers when the player is
        /// both at the room edge AND within the door opening (center of the wall).
        /// </summary>
        public bool IsAtExit(out string direction)
        {
            direction = null;
            float edgeMargin = 0.3f;
            Vector2 localPos = (Vector2)transform.position - roomCenter;

            // Check each edge — player must be near the edge AND centered in the door zone
            if (localPos.y >= roomMax.y - edgeMargin && IsInDoorZoneH(localPos.x))
            { direction = "up"; return true; }

            if (localPos.y <= roomMin.y + edgeMargin && IsInDoorZoneH(localPos.x))
            { direction = "down"; return true; }

            if (localPos.x <= roomMin.x + edgeMargin && IsInDoorZoneV(localPos.y))
            { direction = "left"; return true; }

            if (localPos.x >= roomMax.x - edgeMargin && IsInDoorZoneV(localPos.y))
            { direction = "right"; return true; }

            return false;
        }

        /// <summary>
        /// Is the X position within the horizontal door zone (center of top/bottom wall)?
        /// </summary>
        private bool IsInDoorZoneH(float localX)
        {
            return Mathf.Abs(localX) <= doorWidth;
        }

        /// <summary>
        /// Is the Y position within the vertical door zone (center of left/right wall)?
        /// </summary>
        private bool IsInDoorZoneV(float localY)
        {
            return Mathf.Abs(localY) <= doorWidth;
        }

        public void SetPosition(Vector2 pos)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (rb != null) rb.position = pos;
        }
    }
}
