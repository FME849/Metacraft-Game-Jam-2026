using UnityEngine;
using UnityEngine.UI;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(Canvas))]
    public sealed class AspectRatioBlackBars : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float preferredAspect = 16f / 9f;
        [SerializeField] private Color barColor = Color.black;
        [SerializeField] private bool blockRaycasts = true;

        private RectTransform leftBar;
        private RectTransform rightBar;
        private Image leftImage;
        private Image rightImage;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            EnsureBars();
            UpdateBars();
        }

        private void Update()
        {
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            {
                return;
            }

            UpdateBars();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateBars();
        }

        private void EnsureBars()
        {
            leftBar = EnsureBar("LeftBlackBar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), out leftImage);
            rightBar = EnsureBar("RightBlackBar", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), out rightImage);
        }

        private RectTransform EnsureBar(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            out Image image)
        {
            Transform existing = transform.Find(objectName);
            GameObject barObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barObject.transform.SetParent(transform, false);

            RectTransform rectTransform = barObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;

            image = barObject.GetComponent<Image>();
            image.raycastTarget = blockRaycasts;
            image.color = barColor;

            return rectTransform;
        }

        private void UpdateBars()
        {
            EnsureBars();

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (lastScreenWidth <= 0 || lastScreenHeight <= 0)
            {
                return;
            }

            float currentAspect = (float)lastScreenWidth / lastScreenHeight;
            float barWidth = currentAspect < preferredAspect
                ? 0f
                : (lastScreenWidth - lastScreenHeight * preferredAspect) * 0.5f;

            SetBar(leftBar, leftImage, barWidth);
            SetBar(rightBar, rightImage, barWidth);
        }

        private void SetBar(RectTransform rectTransform, Image image, float width)
        {
            rectTransform.sizeDelta = new Vector2(width, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            image.color = barColor;
            image.raycastTarget = blockRaycasts;
            rectTransform.gameObject.SetActive(width > 0.01f);
        }
    }
}
