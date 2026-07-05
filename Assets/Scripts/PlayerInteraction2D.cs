using UnityEngine;
using UnityEngine.InputSystem;

namespace Metacraft.Interaction
{
    public sealed class PlayerInteraction2D : MonoBehaviour
    {
        private Interactable2D currentInteractable;

        private void Update()
        {
            if (currentInteractable != null && !currentInteractable.CanInteract)
            {
                SetCurrentInteractable(null);
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || currentInteractable == null)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                currentInteractable.Interact(gameObject);
            }
        }

        public void NotifyTriggerEntered(Collider2D other)
        {
            Interactable2D interactable = other.GetComponentInParent<Interactable2D>();
            if (interactable == null || !interactable.CanInteract)
            {
                return;
            }

            SetCurrentInteractable(interactable);
        }

        public void NotifyTriggerExited(Collider2D other)
        {
            Interactable2D interactable = other.GetComponentInParent<Interactable2D>();
            if (currentInteractable == null || interactable == null)
            {
                return;
            }

            if (currentInteractable == interactable)
            {
                SetCurrentInteractable(null);
            }
        }

        private void SetCurrentInteractable(Interactable2D interactable)
        {
            if (currentInteractable == interactable)
            {
                return;
            }

            if (currentInteractable != null)
            {
                currentInteractable.SetPromptVisible(false);
            }

            currentInteractable = interactable;

            if (currentInteractable != null)
            {
                currentInteractable.SetPromptVisible(true);
            }
        }
    }
}
