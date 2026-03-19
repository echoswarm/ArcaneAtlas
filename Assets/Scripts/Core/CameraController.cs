using UnityEngine;

namespace ArcaneAtlas.Core
{
    public class CameraController : MonoBehaviour
    {
        public float slideDuration = 0.5f;

        private bool isSliding = false;
        private Vector3 slideFrom;
        private Vector3 slideTo;
        private float slideTimer;

        public bool IsSliding => isSliding;

        public void SlideTo(Vector3 targetPosition)
        {
            slideFrom = transform.position;
            slideTo = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            slideTimer = 0f;
            isSliding = true;
        }

        void Update()
        {
            if (!isSliding) return;

            slideTimer += Time.deltaTime;
            float t = Mathf.Clamp01(slideTimer / slideDuration);
            t = t * t * (3f - 2f * t); // smoothstep
            transform.position = Vector3.Lerp(slideFrom, slideTo, t);

            if (t >= 1f)
            {
                isSliding = false;
            }
        }

        public void SnapTo(Vector3 targetPosition)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            isSliding = false;
        }
    }
}
