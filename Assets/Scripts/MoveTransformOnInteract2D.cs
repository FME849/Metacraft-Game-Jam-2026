using System.Collections;
using Metacraft.Dialogue;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.Interaction
{
    [RequireComponent(typeof(Interactable2D))]
    public sealed class MoveTransformOnInteract2D : MonoBehaviour
    {
        [SerializeField] private Transform movingTransform;
        [SerializeField] private Transform targetPoint;
        [SerializeField, Min(0.01f)] private float duration = 1f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Range(0f, 1f)] private float fadeTargetAlpha = 0.5f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.75f;
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string dialogueNodeName;
        [SerializeField, Min(0)] private int dialogueStartLineIndex;
        [SerializeField] private int dialogueLineCount = -1;
        [SerializeField] private bool lockPlayerControlsWhenOpened = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";
        [SerializeField] private bool triggerOnce = true;

        private Interactable2D interactable;
        private Coroutine moveRoutine;
        private Coroutine fadeRoutine;
        private Coroutine sequenceRoutine;
        private bool hasTriggered;
        private bool dialogueCompleted;
        private bool controlsLockedBySequence;

        private void Awake()
        {
            interactable = GetComponent<Interactable2D>();
        }

        private void OnEnable()
        {
            if (interactable == null)
            {
                interactable = GetComponent<Interactable2D>();
            }

            interactable.Interacted += HandleInteracted;
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= HandleInteracted;
            }
        }

        private void HandleInteracted(GameObject interactor)
        {
            if ((triggerOnce && hasTriggered) || movingTransform == null || targetPoint == null)
            {
                return;
            }

            hasTriggered = true;

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
            }

            sequenceRoutine = StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            if (dialoguePresenter != null && !string.IsNullOrWhiteSpace(dialogueNodeName))
            {
                dialogueCompleted = false;
                dialoguePresenter.Play(
                    dialogueNodeName,
                    dialogueStartLineIndex,
                    dialogueLineCount,
                    () => dialogueCompleted = true);

                while (!dialogueCompleted)
                {
                    yield return null;
                }
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            LockPlayerControls();
            FadeTo(fadeTargetAlpha, fadeDuration);
            moveRoutine = StartCoroutine(MoveToTarget());
            sequenceRoutine = null;
        }

        private void LockPlayerControls()
        {
            if (!lockPlayerControlsWhenOpened || controlsLockedBySequence)
            {
                return;
            }

            ResolvePlayerMovement();
            if (playerMovement == null)
            {
                return;
            }

            playerMovement.SetControlsLocked(true);
            controlsLockedBySequence = true;
        }

        private void ResolvePlayerMovement()
        {
            if (playerMovement != null || string.IsNullOrWhiteSpace(playerObjectName))
            {
                return;
            }

            GameObject playerObject = GameObject.Find(playerObjectName);
            if (playerObject != null)
            {
                playerMovement = playerObject.GetComponentInChildren<SimplePlayerMovement2D>();
            }
        }

        public void FadeTo(float targetAlpha, float durationSeconds)
        {
            if (fadeGroup == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(FadeOverlay(targetAlpha, durationSeconds));
        }

        private IEnumerator MoveToTarget()
        {
            Vector3 startPosition = movingTransform.position;
            Vector3 targetPosition = targetPoint.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float curveValue = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;
                movingTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
                yield return null;
            }

            movingTransform.position = targetPosition;
            moveRoutine = null;
        }

        private IEnumerator FadeOverlay(float targetAlpha, float durationSeconds)
        {
            fadeGroup.gameObject.SetActive(true);
            fadeGroup.blocksRaycasts = targetAlpha > 0.01f;

            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < durationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durationSeconds);
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
            fadeGroup.gameObject.SetActive(targetAlpha > 0.01f);
            fadeRoutine = null;
        }
    }
}
