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
        [SerializeField] private string[] lines;
        [SerializeField, Min(1f)] private float scrollSpeed = 50f;
        [SerializeField] private string nextSceneName;
        [SerializeField] private AudioClip backgroundMusic;

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

                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            }
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = crawlText != null && crawlContent != null && viewport != null && audioSource != null;
            if (!hasReferences)
            {
                Debug.LogWarning("TextCrawl needs Crawl Text, Crawl Content, Viewport, and Audio Source references assigned in the Inspector.", this);
            }

            return hasReferences;
        }
    }
}
