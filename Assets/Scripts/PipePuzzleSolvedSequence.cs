using System.Collections;
using Metacraft.Puzzles;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    public sealed class PipePuzzleSolvedSequence : MonoBehaviour
    {
        [SerializeField] private SlidingPipePuzzle2D puzzle;
        [SerializeField] private GameObject puzzleRoot;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip steamSound;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.75f;
        [SerializeField] private GameObject[] objectsToEnableOnSolved;

        private bool hasPlayed;

        private void OnEnable()
        {
            if (puzzle != null)
            {
                puzzle.Solved += HandlePuzzleSolved;
            }
        }

        private void OnDisable()
        {
            if (puzzle != null)
            {
                puzzle.Solved -= HandlePuzzleSolved;
            }
        }

        private void HandlePuzzleSolved()
        {
            if (hasPlayed)
            {
                return;
            }

            hasPlayed = true;
            StartCoroutine(PlaySteamThenHide());
        }

        private IEnumerator PlaySteamThenHide()
        {
            if (audioSource != null && steamSound != null)
            {
                audioSource.PlayOneShot(steamSound);
                yield return new WaitForSecondsRealtime(steamSound.length);
            }
            else if (steamSound != null)
            {
                AudioSource temporarySource = PlayOneShot2D(steamSound);
                while (temporarySource != null && temporarySource.isPlaying)
                {
                    yield return null;
                }
            }

            yield return HidePuzzleAndFade();
        }

        private static AudioSource PlayOneShot2D(AudioClip clip)
        {
            GameObject audioObject = new GameObject("PipePuzzleSteamSound");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.clip = clip;
            source.Play();
            Destroy(audioObject, clip.length + 0.25f);
            return source;
        }

        private IEnumerator HidePuzzleAndFade()
        {
            if (puzzleRoot != null)
            {
                puzzleRoot.SetActive(false);
            }

            if (fadeGroup == null)
            {
                EnableSolvedObjects();
                yield break;
            }

            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                fadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.gameObject.SetActive(false);
            EnableSolvedObjects();
        }

        private void EnableSolvedObjects()
        {
            if (objectsToEnableOnSolved == null)
            {
                return;
            }

            for (int i = 0; i < objectsToEnableOnSolved.Length; i++)
            {
                if (objectsToEnableOnSolved[i] != null)
                {
                    objectsToEnableOnSolved[i].SetActive(true);
                }
            }
        }
    }
}
