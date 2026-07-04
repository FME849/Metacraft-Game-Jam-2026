using UnityEngine;

namespace Metacraft.Player
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationDriver2D : MonoBehaviour
    {
        [SerializeField] private float movingThreshold = 0.02f;
        [SerializeField] private string animationStateName = "WalkAnimation";
        [SerializeField, Min(0f)] private float animationSpeedMultiplier = 1f;
        [SerializeField] private bool flipByMoveDirection = true;
        [SerializeField] private bool invertFlipDirection;
        [SerializeField] private Transform flipRoot;

        private Animator animator;
        private Vector3 previousPosition;
        private float defaultScaleX;
        private int animationStateHash;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            previousPosition = transform.position;

            if (flipRoot == null)
            {
                flipRoot = transform;
            }

            defaultScaleX = Mathf.Abs(flipRoot.localScale.x);
            animationStateHash = Animator.StringToHash(animationStateName);
            animator.Play(animationStateHash, 0, 0f);
            animator.speed = 0f;
        }

        private void Update()
        {
            Vector3 currentPosition = transform.position;
            float horizontalDelta = currentPosition.x - previousPosition.x;
            float horizontalSpeed = Mathf.Abs(horizontalDelta) / Mathf.Max(Time.deltaTime, 0.0001f);
            bool isMoving = horizontalSpeed > movingThreshold;

            animator.speed = isMoving ? animationSpeedMultiplier : 0f;

            if (!isMoving)
            {
                animator.Play(animationStateHash, 0, 0f);
            }
            else if (flipByMoveDirection && Mathf.Abs(horizontalDelta) > 0.0001f)
            {
                float direction = Mathf.Sign(horizontalDelta);
                if (invertFlipDirection)
                {
                    direction *= -1f;
                }

                Vector3 scale = flipRoot.localScale;
                scale.x = direction * defaultScaleX;
                flipRoot.localScale = scale;
            }

            previousPosition = currentPosition;
        }
    }
}
