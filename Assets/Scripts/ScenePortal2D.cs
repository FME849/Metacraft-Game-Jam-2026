using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metacraft.SceneFlow
{
    public sealed class ScenePortal2D : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "Scene02";
        [SerializeField] private string playerObjectName = "MainPlayer";
        [SerializeField] private string playerTag;
        [SerializeField] private SceneFadeOut fadeOut;

        private bool loading;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (loading || !IsPlayer(other))
            {
                return;
            }

            loading = true;
            StartCoroutine(LoadSceneAfterFade());
        }

        private IEnumerator LoadSceneAfterFade()
        {
            if (fadeOut == null)
            {
                fadeOut = FindSceneFadeOut();
            }

            if (fadeOut != null)
            {
                yield return fadeOut.FadeOut();
            }

            SceneManager.LoadScene(targetSceneName);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(playerObjectName))
            {
                return false;
            }

            return other.name == playerObjectName ||
                other.transform.root.name == playerObjectName ||
                other.GetComponentInParent<Transform>().root.name == playerObjectName;
        }

        private static SceneFadeOut FindSceneFadeOut()
        {
            SceneFadeOut[] candidates = Resources.FindObjectsOfTypeAll<SceneFadeOut>();
            for (int i = 0; i < candidates.Length; i++)
            {
                SceneFadeOut candidate = candidates[i];
                if (candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
