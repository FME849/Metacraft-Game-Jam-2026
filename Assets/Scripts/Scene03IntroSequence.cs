using System.Collections;
using Metacraft.Dialogue;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [DefaultExecutionOrder(1000)]
    public sealed class Scene03IntroSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene03_Dialogue";
        [SerializeField, Min(0f)] private float startDelay = 1.4f;
        [SerializeField, Min(1)] private int openingLineCount = 1;
        [SerializeField] private SceneFadeIn fadeIn;
        [SerializeField, Min(1)] private int dialogueLinesBeforeNurseMove = 1;
        [SerializeField, Min(0f)] private float delayBeforeAnimation = 0.5f;
        [SerializeField] private NpcMoveToPoint2D nurseMovement;
        [SerializeField] private Transform nurseMoveTarget;
        [SerializeField, Min(0f)] private float delayAfterAnimation = 0.5f;
        [SerializeField, Min(0)] private int dialogueLinesAfterNurseMove = 24;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";

        private bool dialogueCompleted;
        private bool controlsLockedBySequence;

        private void Awake()
        {
            LockPlayerControls();
        }

        private void OnDisable()
        {
            UnlockPlayerControls();
        }

        private IEnumerator Start()
        {
            if (dialoguePresenter == null)
            {
                Debug.LogWarning("Scene03IntroSequence needs a Dialogue Presenter reference.", this);
                UnlockPlayerControls();
                yield break;
            }

            LockPlayerControls();

            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            yield return PlayDialogueSegment(0, openingLineCount);

            if (fadeIn != null)
            {
                yield return fadeIn.FadeIn();
            }

            yield return PlayDialogueSegment(openingLineCount, dialogueLinesBeforeNurseMove);

            if (delayBeforeAnimation > 0f)
            {
                yield return new WaitForSeconds(delayBeforeAnimation);
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

            if (delayAfterAnimation > 0f)
            {
                yield return new WaitForSeconds(delayAfterAnimation);
            }

            yield return PlayDialogueSegment(
                openingLineCount + dialogueLinesBeforeNurseMove,
                dialogueLinesAfterNurseMove);

            UnlockPlayerControls();
        }

        private IEnumerator PlayDialogueSegment(int startLineIndex, int maxLines)
        {
            dialogueCompleted = false;
            dialoguePresenter.Play(nodeName, startLineIndex, maxLines, () => dialogueCompleted = true);

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
