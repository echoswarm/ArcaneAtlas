using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneAtlas.Core
{
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

        void Update()
        {
            moveInput = moveAction.ReadValue<Vector2>();
            bool isMoving = moveInput.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed * Time.deltaTime;
                Vector3 newPos = transform.position + delta;

                newPos.x = Mathf.Clamp(newPos.x, roomCenter.x + roomMin.x, roomCenter.x + roomMax.x);
                newPos.y = Mathf.Clamp(newPos.y, roomCenter.y + roomMin.y, roomCenter.y + roomMax.y);

                transform.position = newPos;

                if (moveInput.x < 0) spriteRenderer.flipX = true;
                else if (moveInput.x > 0) spriteRenderer.flipX = false;
            }
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
        }
    }
}
