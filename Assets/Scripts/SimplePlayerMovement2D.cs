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

        private Rigidbody2D rb;
        private float moveInput;
        private int controlLockCount;

        public bool ControlsLocked => controlLockCount > 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
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
        }

        private void FixedUpdate()
        {
            float targetSpeed = moveInput * moveSpeed;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float nextX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);
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

        private void ClearBufferedInput()
        {
            moveInput = 0f;
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
