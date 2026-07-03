using UnityEngine;

namespace Metacraft.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class InteractionDetector2D : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction2D playerInteraction;

        private void Awake()
        {
            if (playerInteraction == null)
            {
                playerInteraction = GetComponentInParent<PlayerInteraction2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            playerInteraction?.NotifyTriggerEntered(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            playerInteraction?.NotifyTriggerExited(other);
        }
    }
}
