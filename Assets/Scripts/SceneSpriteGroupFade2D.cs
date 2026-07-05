using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    public sealed class SceneSpriteGroupFade2D : MonoBehaviour
    {
        [SerializeField] private Transform[] targets;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField, Min(0f)] private float eyesFadeDelay = 1f;

        private readonly List<SpriteRenderer> renderers = new();
        private readonly List<Color> visibleColors = new();
        private readonly List<bool> rendererIsEyes = new();

        private void Awake()
        {
            CacheRenderers();

            if (hideOnAwake)
            {
                SetAlpha(0f);
            }
        }

        public IEnumerator FadeIn(float duration)
        {
            if (renderers.Count == 0)
            {
                CacheRenderers();
            }

            if (duration <= 0f)
            {
                SetAlpha(1f);
                yield break;
            }

            float elapsed = 0f;
            float totalDuration = duration + eyesFadeDelay;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(
                    Mathf.Clamp01(elapsed / duration),
                    Mathf.Clamp01((elapsed - eyesFadeDelay) / duration));
                yield return null;
            }

            SetAlpha(1f);
        }

        private void CacheRenderers()
        {
            renderers.Clear();
            visibleColors.Clear();
            rendererIsEyes.Clear();

            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                SpriteRenderer[] targetRenderers = targets[i].GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < targetRenderers.Length; j++)
                {
                    renderers.Add(targetRenderers[j]);

                    Color color = targetRenderers[j].color;
                    color.a = color.a > 0f ? color.a : 1f;
                    visibleColors.Add(color);
                    rendererIsEyes.Add(IsEyesRenderer(targetRenderers[j].transform));
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            SetAlpha(alpha, alpha);
        }

        private void SetAlpha(float bodyAlpha, float eyesAlpha)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                Color color = visibleColors[i];
                color.a *= rendererIsEyes[i] ? eyesAlpha : bodyAlpha;
                renderers[i].color = color;
            }
        }

        private static bool IsEyesRenderer(Transform rendererTransform)
        {
            Transform current = rendererTransform;
            while (current != null)
            {
                if (current.name == "Eyes")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
