using UnityEngine;

namespace Metacraft.Animation
{
    /// <summary>
    /// Adds cheap secondary motion on top of keyed animation.
    /// Attach this to a child body part, then assign the character root as Motion Source.
    /// </summary>
    public sealed class ProceduralWobble2D : MonoBehaviour
    {
        [Header("Motion Source")]
        [SerializeField] private Transform motionSource;
        [SerializeField] private bool useSourceVelocity = true;

        [Header("Rotation")]
        [SerializeField] private bool wobbleRotation = true;
        [SerializeField, Min(0f)] private float rotationAmount = 10f;
        [SerializeField, Min(0.01f)] private float rotationSpring = 28f;
        [SerializeField, Min(0.01f)] private float rotationDamping = 9f;
        [SerializeField] private float rotationNoise = 1.5f;

        [Header("Position Lag")]
        [SerializeField] private bool wobblePosition = true;
        [SerializeField, Min(0f)] private float positionAmount = 0.05f;
        [SerializeField, Min(0.01f)] private float positionSpring = 34f;
        [SerializeField, Min(0.01f)] private float positionDamping = 10f;

        [Header("Squash")]
        [SerializeField] private bool squashAndStretch;
        [SerializeField, Min(0f)] private float squashAmount = 0.08f;
        [SerializeField, Min(0.01f)] private float squashSpring = 30f;
        [SerializeField, Min(0.01f)] private float squashDamping = 9f;

        [Header("Tuning")]
        [SerializeField] private Vector2 velocityInfluence = new Vector2(-0.25f, 0.18f);
        [SerializeField, Range(0f, 1f)] private float randomPhaseOffset = 0.35f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;
        private Vector3 previousSourcePosition;

        private float rotationOffset;
        private float rotationVelocity;
        private Vector2 positionOffset;
        private Vector2 positionVelocity;
        private float squashOffset;
        private float squashVelocity;
        private float impulse;
        private float phase;

        public void AddImpulse(float amount)
        {
            impulse += amount;
        }

        public void SetMotionSource(Transform source)
        {
            motionSource = source;
            previousSourcePosition = source != null ? source.position : transform.position;
        }

        public void SetSquashAndStretch(bool enabled)
        {
            squashAndStretch = enabled;
        }

        private void Awake()
        {
            CaptureBasePose();
        }

        private void OnEnable()
        {
            CaptureBasePose();
        }

        private void LateUpdate()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0f;
            if (dt <= 0f)
            {
                return;
            }

            Transform source = motionSource != null ? motionSource : transform.parent;
            Vector2 sourceVelocity = GetSourceVelocity(source, dt);
            float speed = sourceVelocity.magnitude;

            if (wobbleRotation)
            {
                float lean = Vector2.Dot(sourceVelocity, velocityInfluence) * rotationAmount;
                float noise = Mathf.Sin((Time.time * 12f) + phase) * rotationNoise * Mathf.Clamp01(speed);
                float targetRotation = lean + impulse + noise;
                rotationOffset = Spring(rotationOffset, targetRotation, ref rotationVelocity, rotationSpring, rotationDamping, dt);
            }

            if (wobblePosition)
            {
                Vector2 targetPosition = -sourceVelocity * positionAmount;
                positionOffset.x = Spring(positionOffset.x, targetPosition.x, ref positionVelocity.x, positionSpring, positionDamping, dt);
                positionOffset.y = Spring(positionOffset.y, targetPosition.y, ref positionVelocity.y, positionSpring, positionDamping, dt);
            }

            if (squashAndStretch)
            {
                float targetSquash = Mathf.Clamp(speed * squashAmount, -0.25f, 0.25f);
                squashOffset = Spring(squashOffset, targetSquash, ref squashVelocity, squashSpring, squashDamping, dt);
            }

            impulse = Mathf.MoveTowards(impulse, 0f, 120f * dt);

            transform.localPosition = baseLocalPosition + (Vector3)positionOffset;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, rotationOffset);
            transform.localScale = GetSquashedScale();
        }

        private Vector2 GetSourceVelocity(Transform source, float dt)
        {
            if (!useSourceVelocity || source == null)
            {
                return Vector2.zero;
            }

            Vector3 currentPosition = source.position;
            Vector2 velocity = (currentPosition - previousSourcePosition) / dt;
            previousSourcePosition = currentPosition;
            return velocity;
        }

        private Vector3 GetSquashedScale()
        {
            if (!squashAndStretch)
            {
                return baseLocalScale;
            }

            float stretchY = 1f + squashOffset;
            float squashX = 1f - (squashOffset * 0.55f);
            return new Vector3(baseLocalScale.x * squashX, baseLocalScale.y * stretchY, baseLocalScale.z);
        }

        private void CaptureBasePose()
        {
            Transform source = motionSource != null ? motionSource : transform.parent;
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseLocalScale = transform.localScale;
            previousSourcePosition = source != null ? source.position : transform.position;
            phase = Random.value * Mathf.PI * 2f * randomPhaseOffset;
        }

        private static float Spring(float current, float target, ref float velocity, float spring, float damping, float dt)
        {
            float force = (target - current) * spring;
            velocity += force * dt;
            velocity *= Mathf.Exp(-damping * dt);
            return current + (velocity * dt);
        }
    }
}
