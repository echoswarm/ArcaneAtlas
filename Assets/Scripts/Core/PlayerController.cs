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

            // Set up collider — small box at feet for natural collision
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

                // Clamp to room bounds
                targetPos.x = Mathf.Clamp(targetPos.x, roomCenter.x + roomMin.x, roomCenter.x + roomMax.x);
                targetPos.y = Mathf.Clamp(targetPos.y, roomCenter.y + roomMin.y, roomCenter.y + roomMax.y);

                // Physics-based movement — respects colliders
                rb.MovePosition(targetPos);
            }
            else
            {
                // Stop movement when no input
                rb.linearVelocity = Vector2.zero;
            }
        }

        void Update()
        {
            // Sprite flipping in Update (visual only)
            if (moveInput.x < -0.1f && spriteRenderer != null)
                spriteRenderer.flipX = true;
            else if (moveInput.x > 0.1f && spriteRenderer != null)
                spriteRenderer.flipX = false;
        }

        public bool IsAtExit(out string direction)
        {
            direction = null;
            float margin = 0.3f;
            Vector2 localPos = (Vector2)transform.position - roomCenter;
            if (localPos.y >= roomMax.y - margin) { direction = "up"; return true; }
            if (localPos.y <= roomMin.y + margin) { direction = "down"; return true; }
            if (localPos.x <= roomMin.x + margin) { direction = "left"; return true; }
            if (localPos.x >= roomMax.x - margin) { direction = "right"; return true; }
            return false;
        }

        public void SetPosition(Vector2 pos)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (rb != null) rb.position = pos;
        }
    }
}
