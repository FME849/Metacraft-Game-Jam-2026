using System.Collections;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicFadeIn : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float delay = 0.1f;
        [SerializeField, Min(0.01f)] private float duration = 1.2f;

        private AudioSource audioSource;
        private float targetVolume;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            targetVolume = audioSource.volume;
            audioSource.volume = 0f;
        }

        private IEnumerator Start()
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            audioSource.volume = targetVolume;
        }
    }
}
