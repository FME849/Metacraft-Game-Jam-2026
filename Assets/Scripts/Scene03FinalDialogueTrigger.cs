using System.Collections;
using Metacraft.Dialogue;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Scene03FinalDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene03_Dialogue";
        [SerializeField, Min(0)] private int firstLineIndex = 26;
        [SerializeField, Min(1)] private int firstLineCount = 1;
        [SerializeField] private int lineCountAfterAnimation = -1;
        [SerializeField, Min(0f)] private float delayBeforeAnimation = 0.5f;
        [SerializeField] private Transform nurseRoot;
        [SerializeField] private Vector3 nurseTurnEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 nurseReturnEuler = Vector3.zero;
        [SerializeField, Min(0f)] private float delayAfterAnimation = 0.5f;
        [SerializeField] private NpcMoveToPoint2D nurseMovement;
        [SerializeField] private Transform nurseMoveTarget;
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

            if (ShouldRunNurseTurn())
            {
                yield return WaitBeforeAnimationDelay();
                nurseRoot.localRotation = Quaternion.Euler(nurseTurnEuler);
                yield return WaitAfterAnimationDelay();
            }

            if (lineCountAfterAnimation != 0)
            {
                yield return PlayDialogueSegment(firstLineIndex + firstLineCount, lineCountAfterAnimation);
            }

            if (ShouldRunNurseReturn())
            {
                yield return WaitBeforeAnimationDelay();
                nurseRoot.localRotation = Quaternion.Euler(nurseReturnEuler);
            }

            if (nurseMovement != null)
            {
                if (nurseMoveTarget != null)
                {
                    nurseMovement.MoveTo(nurseMoveTarget);
                }
                else
                {
                    nurseMovement.MoveNow();
                }

                while (nurseMovement.IsMoving)
                {
                    yield return null;
                }
            }

            UnlockPlayerControls();
        }

        private bool ShouldRunNurseTurn()
        {
            return nurseRoot != null;
        }

        private bool ShouldRunNurseReturn()
        {
            return nurseRoot != null;
        }

        private IEnumerator PlayDialogueSegment(int startLineIndex, int maxLines)
        {
            if (dialoguePresenter == null)
            {
                yield break;
            }

            dialogueCompleted = false;
            dialoguePresenter.Play(nodeName, startLineIndex, maxLines, () => dialogueCompleted = true);

            while (!dialogueCompleted)
            {
                yield return null;
            }
        }

        private IEnumerator WaitBeforeAnimationDelay()
        {
            if (delayBeforeAnimation > 0f)
            {
                yield return new WaitForSeconds(delayBeforeAnimation);
            }
        }

        private IEnumerator WaitAfterAnimationDelay()
        {
            if (delayAfterAnimation > 0f)
            {
                yield return new WaitForSeconds(delayAfterAnimation);
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
