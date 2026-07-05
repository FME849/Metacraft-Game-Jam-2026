using System.Collections;
using Metacraft.Dialogue;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Scene05DoubleDialogueTriggerSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene05_Dialogue";
        [SerializeField, Min(0)] private int firstLineIndex = 34;
        [SerializeField, Min(1)] private int firstLineCount = 11;
        [SerializeField] private Transform doubleRoot;
        [SerializeField] private Vector3 doubleTurnEuler = Vector3.zero;
        [SerializeField, Min(0)] private int turnLineIndex = 45;
        [SerializeField, Min(1)] private int turnLineCount = 1;
        [SerializeField] private NpcMoveToPoint2D doubleMovement;
        [SerializeField] private Transform doubleMoveTarget;
        [SerializeField, Min(0)] private int finalLineIndex = 46;
        [SerializeField] private int finalLineCount = -1;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";

        private bool hasTriggered;
        private bool dialogueCompleted;
        private bool controlsLockedBySequence;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnDisable()
        {
            UnlockPlayerControls();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((triggerOnce && hasTriggered) || other.GetComponentInParent<SimplePlayerMovement2D>() == null)
            {
                return;
            }

            hasTriggered = true;
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            LockPlayerControls();

            yield return PlayDialogueSegment(firstLineIndex, firstLineCount);

            if (doubleRoot != null)
            {
                doubleRoot.localEulerAngles = doubleTurnEuler;
            }

            if (doubleMovement != null)
            {
                doubleMovement.InvertFlipDirection = false;
            }

            yield return PlayDialogueSegment(turnLineIndex, turnLineCount);

            if (doubleMovement != null && doubleMoveTarget != null)
            {
                doubleMovement.MoveTo(doubleMoveTarget);
                while (doubleMovement.IsMoving)
                {
                    yield return null;
                }
            }

            if (finalLineCount != 0)
            {
                yield return PlayDialogueSegment(finalLineIndex, finalLineCount);
            }

            UnlockPlayerControls();
            gameObject.SetActive(false);
        }

        private IEnumerator PlayDialogueSegment(int startLineIndex, int lineCount)
        {
            if (dialoguePresenter == null)
            {
                yield break;
            }

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
