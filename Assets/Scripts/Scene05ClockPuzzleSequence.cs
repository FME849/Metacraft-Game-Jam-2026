using System.Collections;
using Metacraft.Dialogue;
using Metacraft.Interaction;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(Interactable2D))]
    public sealed class Scene05ClockPuzzleSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene05_Dialogue";
        [SerializeField, Min(0)] private int prePuzzleLineIndex = 15;
        [SerializeField, Min(1)] private int prePuzzleLineCount = 10;
        [SerializeField, Min(0)] private int postPuzzleLineIndex = 25;
        [SerializeField, Min(1)] private int postPuzzleLineCount = 9;
        [SerializeField] private GameObject clockPuzzleRoot;
        [SerializeField] private bool autoCompleteWhenNoPuzzle = true;
        [SerializeField] private bool hideClockPuzzleOnComplete = true;
        [SerializeField] private NpcMoveToPoint2D doubleMovement;
        [SerializeField] private Transform doubleMoveTarget;
        [SerializeField] private GameObject dialogueTriggerToEnable;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";

        private Interactable2D interactable;
        private bool dialogueCompleted;
        private bool puzzleCompleted;
        private bool hasTriggered;
        private bool controlsLockedBySequence;

        private void Awake()
        {
            interactable = GetComponent<Interactable2D>();
            if (clockPuzzleRoot != null)
            {
                clockPuzzleRoot.SetActive(false);
            }

            if (dialogueTriggerToEnable != null)
            {
                dialogueTriggerToEnable.SetActive(false);
            }
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

            UnlockPlayerControls();
        }

        public void CompleteClockPuzzle()
        {
            puzzleCompleted = true;
        }

        private void HandleInteracted(GameObject interactor)
        {
            if ((triggerOnce && hasTriggered) || dialoguePresenter == null)
            {
                return;
            }

            hasTriggered = true;
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            LockPlayerControls();
            if (interactable != null)
            {
                interactable.SetPromptVisible(false);
            }

            yield return PlayDialogueSegment(prePuzzleLineIndex, prePuzzleLineCount);

            if (clockPuzzleRoot != null)
            {
                clockPuzzleRoot.SetActive(true);
            }
            else if (autoCompleteWhenNoPuzzle)
            {
                puzzleCompleted = true;
            }

            while (!puzzleCompleted)
            {
                yield return null;
            }

            if (hideClockPuzzleOnComplete && clockPuzzleRoot != null)
            {
                clockPuzzleRoot.SetActive(false);
            }

            yield return PlayDialogueSegment(postPuzzleLineIndex, postPuzzleLineCount);

            if (dialogueTriggerToEnable != null)
            {
                dialogueTriggerToEnable.SetActive(true);
            }

            if (doubleMovement != null && doubleMoveTarget != null)
            {
                doubleMovement.MoveTo(doubleMoveTarget);
            }

            if (interactable != null)
            {
                interactable.SetInteractionEnabled(false);
            }

            UnlockPlayerControls();
        }

        private IEnumerator PlayDialogueSegment(int startLineIndex, int lineCount)
        {
            dialogueCompleted = false;
            dialoguePresenter.Play(nodeName, startLineIndex, lineCount, () => dialogueCompleted = true);

            while (!dialogueCompleted)
            {
                yield return null;
            }
        }

        private void LockPlayerControls()
        {
            if (controlsLockedBySequence)
            {
                return;
            }

            ResolvePlayerMovement();
            if (playerMovement == null)
            {
                return;
            }

            playerMovement.SetControlsLocked(true);
            controlsLockedBySequence = true;
        }

        private void UnlockPlayerControls()
        {
            if (!controlsLockedBySequence || playerMovement == null)
            {
                return;
            }

            playerMovement.SetControlsLocked(false);
            controlsLockedBySequence = false;
        }

        private void ResolvePlayerMovement()
        {
            if (playerMovement != null || string.IsNullOrWhiteSpace(playerObjectName))
            {
                return;
            }

            GameObject playerObject = GameObject.Find(playerObjectName);
            if (playerObject != null)
            {
                playerMovement = playerObject.GetComponentInChildren<SimplePlayerMovement2D>();
            }
        }
    }
}
