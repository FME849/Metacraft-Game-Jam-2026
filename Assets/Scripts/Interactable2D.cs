using System;
using UnityEngine;

namespace Metacraft.Interaction
{
    public sealed class Interactable2D : MonoBehaviour
    {
        [SerializeField] private GameObject prompt;
        [SerializeField] private string interactionLabel = "Interact";

        public Transform Transform => transform;
        public bool CanInteract => enabled && gameObject.activeInHierarchy;
        public event Action<GameObject> Interacted;

        private void Awake()
        {
            SetPromptVisible(false);
        }

        public void SetPromptVisible(bool visible)
        {
            if (prompt != null)
            {
                prompt.SetActive(visible);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            Debug.Log($"{interactor.name} interacted with {name}: {interactionLabel}", this);
            Interacted?.Invoke(interactor);
        }

        public void SetInteractionEnabled(bool value)
        {
            if (!value)
            {
                SetPromptVisible(false);
            }

            enabled = value;
        }
    }
}
