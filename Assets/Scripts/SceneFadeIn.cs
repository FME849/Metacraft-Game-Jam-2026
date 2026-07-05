using System.Collections;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneFadeIn : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float delay = 0.1f;
        [SerializeField, Min(0.01f)] private float duration = 1.2f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool disableWhenFinished = true;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        private IEnumerator Start()
        {
            if (!playOnStart)
            {
                yield break;
            }

            yield return FadeIn(true);
        }

        public IEnumerator FadeIn(bool includeDelay = false)
        {
            if (includeDelay && delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            if (disableWhenFinished)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
