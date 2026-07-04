using UnityEngine;

namespace Metacraft.NPC
{
    public sealed class NpcMoveToPoint2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float moveSpeed = 1.4f;
        [SerializeField, Min(0.001f)] private float stopDistance = 0.03f;
        [SerializeField] private bool moveOnStart = true;
        [SerializeField] private bool keepOriginalY;
        [SerializeField] private Animator animator;
        [SerializeField] private string walkStateName = "WalkAnimation";
        [SerializeField, Min(0f)] private float animationSpeedMultiplier = 1f;
        [SerializeField] private bool flipByMoveDirection = true;
        [SerializeField] private Transform flipRoot;

        private Rigidbody2D body;
        private bool moving;
        private int walkStateHash;
        private float defaultScaleX;
        private bool warnedMissingWalkState;
        private bool wasWalking;

        public bool IsMoving => moving;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (flipRoot == null && animator != null)
            {
                flipRoot = animator.transform;
            }

            if (flipRoot != null)
            {
                defaultScaleX = Mathf.Abs(flipRoot.localScale.x);
            }

            walkStateHash = Animator.StringToHash(walkStateName);
        }

        private void Start()
        {
            moving = moveOnStart && target != null;
            SetWalkAnimation(moving, 0f);
        }

        private void FixedUpdate()
        {
            if (!moving || target == null)
            {
                return;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 targetPosition = target.position;
            if (keepOriginalY)
            {
                targetPosition.y = currentPosition.y;
            }

            float directionX = targetPosition.x - currentPosition.x;

            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                targetPosition,
                moveSpeed * Time.fixedDeltaTime);

            if (body != null)
            {
                body.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            if (Vector2.Distance(nextPosition, targetPosition) <= stopDistance)
            {
                moving = false;
                SetWalkAnimation(false, directionX);
                return;
            }

            SetWalkAnimation(true, directionX);
        }

        public void MoveNow()
        {
            moving = target != null;
            SetWalkAnimation(moving, 0f);
        }

        private void SetWalkAnimation(bool isWalking, float directionX)
        {
            if (animator == null)
            {
                return;
            }

            bool hasWalkState = !string.IsNullOrWhiteSpace(walkStateName) && animator.HasState(0, walkStateHash);
            if (isWalking && !wasWalking && hasWalkState)
            {
                animator.Play(walkStateHash, 0, 0f);
            }
            else if (isWalking && !hasWalkState && !warnedMissingWalkState)
            {
                Debug.LogWarning($"Animator does not have state '{walkStateName}'.", animator);
                warnedMissingWalkState = true;
            }

            animator.speed = isWalking ? animationSpeedMultiplier : 0f;
            if (!isWalking && wasWalking)
            {
                ResetWalkPose(hasWalkState);
            }

            wasWalking = isWalking;

            if (isWalking && flipByMoveDirection && flipRoot != null && Mathf.Abs(directionX) > 0.0001f)
            {
                Vector3 scale = flipRoot.localScale;
                scale.x = Mathf.Sign(directionX) * defaultScaleX;
                flipRoot.localScale = scale;
            }
        }

        private void ResetWalkPose(bool hasWalkState)
        {
            if (hasWalkState)
            {
                animator.Play(walkStateHash, 0, 0f);
                animator.Update(0f);
            }
        }
    }
}
