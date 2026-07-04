using UnityEngine;
using UnityEngine.InputSystem;

namespace Metacraft.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class SimplePlayerMovement2D : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float acceleration = 55f;
        [SerializeField, Min(0f)] private float deceleration = 70f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpVelocity = 9f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.06f;

        private Rigidbody2D rb;
        private CapsuleCollider2D capsule;
        private float moveInput;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private int controlLockCount;

        public bool ControlsLocked => controlLockCount > 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
        }

        private void Update()
        {
            if (ControlsLocked)
            {
                ClearBufferedInput();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = 0f;
                return;
            }

            float left = keyboard.aKey.isPressed ? 1f : 0f;
            float right = keyboard.dKey.isPressed ? 1f : 0f;
            moveInput = right - left;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            bool grounded = IsGrounded();
            coyoteTimer = grounded ? coyoteTime : coyoteTimer - Time.fixedDeltaTime;

            float targetSpeed = moveInput * moveSpeed;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float nextX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
        }

        public void SetControlsLocked(bool locked)
        {
            if (locked)
            {
                controlLockCount++;
            }
            else
            {
                controlLockCount = Mathf.Max(0, controlLockCount - 1);
            }

            if (ControlsLocked)
            {
                ClearBufferedInput();
                StopHorizontalMovement();
            }
        }

        private bool IsGrounded()
        {
            Bounds bounds = capsule.bounds;
            Vector2 origin = bounds.center;
            Vector2 size = bounds.size;
            CapsuleDirection2D direction = capsule.direction;
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(origin, size, direction, 0f, Vector2.down, groundCheckDistance, groundLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider != capsule)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearBufferedInput()
        {
            moveInput = 0f;
            jumpBufferTimer = 0f;
        }

        private void StopHorizontalMovement()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }
}
