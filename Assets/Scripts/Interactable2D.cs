using UnityEngine;

namespace Metacraft.Interaction
{
    public sealed class Interactable2D : MonoBehaviour
    {
        [SerializeField] private GameObject prompt;
        [SerializeField] private string interactionLabel = "Interact";

        public Transform Transform => transform;

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
            Debug.Log($"{interactor.name} interacted with {name}: {interactionLabel}", this);
        }
    }
}
