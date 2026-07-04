using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Metacraft.TextCrawl
{
    public sealed class TextCrawl : MonoBehaviour
    {
        [SerializeField] private TMP_Text crawlText;
        [SerializeField] private RectTransform crawlContent;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Image topFade;
        [SerializeField] private Image bottomFade;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] private string[] lines;
        [SerializeField, Min(1f)] private float scrollSpeed = 50f;
        [SerializeField] private string nextSceneName;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Min(0f)] private float musicFadeOutDuration = 1.2f;

        private float scrollDistance;
        private float traveled;
        private bool finished;

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            crawlText.text = string.Join("\n\n", lines);
            LayoutRebuilder.ForceRebuildLayoutImmediate(crawlContent);

            topFade.sprite = CreateEdgeFadeSprite(edgeAtTextureBottom: false);
            bottomFade.sprite = CreateEdgeFadeSprite(edgeAtTextureBottom: true);

            float viewportHeight = viewport.rect.height;
            float contentHeight = crawlContent.rect.height;
            scrollDistance = TextCrawlMath.ComputeScrollDistance(viewportHeight, contentHeight);
            traveled = 0f;

            Vector2 startPosition = crawlContent.anchoredPosition;
            startPosition.y = -contentHeight;
            crawlContent.anchoredPosition = startPosition;

            if (backgroundMusic != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            float delta = scrollSpeed * Time.deltaTime;
            crawlContent.anchoredPosition += new Vector2(0f, delta);
            traveled += delta;

            if (traveled >= scrollDistance)
            {
                finished = true;
                StartCoroutine(FadeOutMusicAndLoadNextScene());
            }
        }

        private IEnumerator FadeOutMusicAndLoadNextScene()
        {
            if (audioSource.isPlaying && musicFadeOutDuration > 0f)
            {
                float startVolume = audioSource.volume;
                float elapsed = 0f;
                while (elapsed < musicFadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeOutDuration);
                    yield return null;
                }

                audioSource.volume = 0f;
            }

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = crawlText != null && crawlContent != null && viewport != null && audioSource != null
                && topFade != null && bottomFade != null;
            if (!hasReferences)
            {
                Debug.LogWarning("TextCrawl needs Crawl Text, Crawl Content, Viewport, Audio Source, Top Fade, and Bottom Fade references assigned in the Inspector.", this);
            }

            return hasReferences;
        }

        private Sprite CreateEdgeFadeSprite(bool edgeAtTextureBottom)
        {
            const int height = 64;
            var texture = new Texture2D(1, height, TextureFormat.Alpha8, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                float distanceFromEdge = edgeAtTextureBottom ? t : 1f - t;
                float alpha = fadeCurve.Evaluate(distanceFromEdge);
                texture.SetPixel(0, y, new Color(0f, 0f, 0f, alpha));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f));
        }
    }
}
