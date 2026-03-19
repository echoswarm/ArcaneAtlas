using UnityEngine;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    /// <summary>
    /// Simple frame-based sprite animator for characters.
    /// Switches between idle and walk animations based on movement.
    /// Attach to any GameObject with a SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAnimator : MonoBehaviour
    {
        private SpriteRenderer sr;
        private CharacterDef characterDef;

        private Sprite[] currentFrames;
        private float frameRate;
        private float frameTimer;
        private int currentFrame;

        private bool isWalking;
        private Vector3 lastPosition;

        // Idle loop pause — wait this long before replaying idle animation
        private float idlePauseTimer;
        private bool idlePaused;
        private const float IDLE_PAUSE_DURATION = 2.0f;

        // Movement threshold — below this speed, play idle
        private const float MOVE_THRESHOLD = 0.01f;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            lastPosition = transform.position;
        }

        /// <summary>
        /// Sets the character definition and starts the idle animation.
        /// </summary>
        public void SetCharacter(CharacterDef def)
        {
            characterDef = def;
            if (def == null) return;

            // Start with idle
            SetAnimation(false);

            // Flip to face right (Minifantasy sprites default to left-facing)
            sr.flipX = true;

            // Apply first frame immediately
            if (currentFrames != null && currentFrames.Length > 0)
                sr.sprite = currentFrames[0];
        }

        void Update()
        {
            if (characterDef == null || sr == null) return;

            // Detect movement
            float moved = Vector3.Distance(transform.position, lastPosition);
            bool walking = moved > MOVE_THRESHOLD;
            lastPosition = transform.position;

            // Switch animation if state changed
            if (walking != isWalking)
                SetAnimation(walking);

            // Advance frame
            if (currentFrames == null || currentFrames.Length <= 1) return;

            // Idle pause: wait between animation loops
            if (idlePaused)
            {
                idlePauseTimer += Time.deltaTime;
                if (idlePauseTimer >= IDLE_PAUSE_DURATION)
                {
                    idlePaused = false;
                    idlePauseTimer = 0f;
                    currentFrame = 0;
                    sr.sprite = currentFrames[0];
                }
                return;
            }

            frameTimer += Time.deltaTime;
            float interval = 1f / frameRate;
            if (frameTimer >= interval)
            {
                frameTimer -= interval;
                currentFrame++;

                // Check if we've completed a full loop
                if (currentFrame >= currentFrames.Length)
                {
                    currentFrame = 0;

                    // Pause at end of idle loops (not walk)
                    if (!isWalking)
                    {
                        idlePaused = true;
                        idlePauseTimer = 0f;
                        sr.sprite = currentFrames[0]; // Rest on first frame during pause
                        return;
                    }
                }

                sr.sprite = currentFrames[currentFrame];
            }
        }

        private void SetAnimation(bool walking)
        {
            isWalking = walking;

            var walkFrames = characterDef.GetWalkFrames();
            var idleFrames = characterDef.GetIdleFrames();

            if (walking && walkFrames != null && walkFrames.Length > 0)
            {
                currentFrames = walkFrames;
                frameRate = characterDef.WalkFrameRate;
            }
            else if (idleFrames != null && idleFrames.Length > 0)
            {
                currentFrames = idleFrames;
                frameRate = characterDef.IdleFrameRate;
            }
            else
            {
                currentFrames = null;
                return;
            }

            currentFrame = 0;
            frameTimer = 0f;
            idlePaused = false;
            idlePauseTimer = 0f;

            if (currentFrames.Length > 0)
                sr.sprite = currentFrames[0];
        }

        /// <summary>
        /// Returns the current character definition (for save/load reference).
        /// </summary>
        public CharacterDef GetCharacterDef() => characterDef;
    }
}
