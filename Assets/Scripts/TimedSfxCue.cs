using System.Collections;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class TimedSfxCue : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(0f)] private float maxDuration;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

        private AudioSource audioSource;
        private float originalVolume;
        private Coroutine fadeOutCoroutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            originalVolume = audioSource.volume;
        }

        public void Play()
        {
            if (clip == null)
            {
                return;
            }

            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = null;
                audioSource.volume = originalVolume;
            }

            audioSource.clip = clip;
            audioSource.Play();

            if (maxDuration > 0f)
            {
                fadeOutCoroutine = StartCoroutine(StopAfterDuration());
            }
        }

        private IEnumerator StopAfterDuration()
        {
            float waitTime = Mathf.Max(0f, maxDuration - fadeOutDuration);
            yield return new WaitForSeconds(waitTime);

            float startVolume = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = originalVolume;
            fadeOutCoroutine = null;
        }
    }
}
