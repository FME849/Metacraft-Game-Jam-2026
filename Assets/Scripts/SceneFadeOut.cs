using System.Collections;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneFadeOut : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 1.2f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }
}
