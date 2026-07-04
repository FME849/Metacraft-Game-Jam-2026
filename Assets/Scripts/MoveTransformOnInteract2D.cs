using System.Collections;
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
        [SerializeField] private bool triggerOnce = true;

        private Interactable2D interactable;
        private Coroutine moveRoutine;
        private bool hasTriggered;

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

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveToTarget());
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
    }
}
