using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneAtlas.Core
{
    public class ScreenTransition : MonoBehaviour
    {
        public Image fadeImage;
        public float fadeDuration = 0.3f;

        public static ScreenTransition Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = false;
            }
        }

        public void FadeAndExecute(System.Action action)
        {
            StartCoroutine(DoFade(action));
        }

        private IEnumerator DoFade(System.Action action)
        {
            if (fadeImage == null) { action?.Invoke(); yield break; }

            fadeImage.raycastTarget = true;
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, t / fadeDuration);
                yield return null;
            }

            action?.Invoke();

            t = fadeDuration;
            while (t > 0)
            {
                t -= Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, t / fadeDuration);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;
        }
    }
}
