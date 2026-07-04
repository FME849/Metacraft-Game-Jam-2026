using System.Collections;
using Metacraft.Dialogue;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneDialogueTriggerSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene02_Dialogue";
        [SerializeField, Min(1)] private int openingLineCount = 2;
        [SerializeField, Min(0f)] private float delayBeforeAnimation = 0.5f;
        [SerializeField] private Animator animator;
        [SerializeField] private string animationStateName;
        [SerializeField, Min(0f)] private float animationFallbackWait;
        [SerializeField, Min(0f)] private float delayAfterAnimation = 0.5f;
        [SerializeField] private NpcMoveToPoint2D npcMovementAfterAnimation;
        [SerializeField, Min(0)] private int dialogueLinesBeforeReveal = 8;
        [SerializeField] private SceneSpriteGroupFade2D revealGroup;
        [SerializeField, Min(0f)] private float revealFadeDuration = 3f;
        [SerializeField, Min(0f)] private float delayAfterReveal = 0.5f;
        [SerializeField] private Transform doubleTurnRoot;
        [SerializeField] private Vector3 doubleTurnEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private NpcMoveToPoint2D guardMovementDuringReveal;
        [SerializeField] private Transform guardReturnTarget;
        [SerializeField, Min(0f)] private float guardReturnSpeedMultiplier = 1.5f;
        [SerializeField] private bool guardReturnInvertFlipDirection;
        [SerializeField, Min(0)] private int dialogueLinesAfterRevealBeforeAccuserMove = 6;
        [SerializeField] private NpcMoveToPoint2D accuserMovementAfterRevealDialogue;
        [SerializeField] private Transform accuserMoveTarget;
        [SerializeField] private SceneFadeOut endingFadeOut;
        [SerializeField, Min(0f)] private float delayBeforeEndingFadeOut = 1.5f;
        [SerializeField] private string nextSceneName = "Scene03";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";

        private bool hasTriggered;
        private bool dialogueCompleted;
        private bool controlsLockedBySequence;

        private void Reset()
        {
            Collider2D triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
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

            yield return PlayDialogueSegment(0, openingLineCount);

            if (delayBeforeAnimation > 0f)
            {
                yield return new WaitForSeconds(delayBeforeAnimation);
            }

            yield return PlayAnimationState();

            if (npcMovementAfterAnimation != null)
            {
                npcMovementAfterAnimation.MoveNow();
                while (npcMovementAfterAnimation.IsMoving)
                {
                    yield return null;
                }

                yield return WaitAfterAnimationDelay();
            }

            yield return PlayDialogueSegment(openingLineCount, dialogueLinesBeforeReveal);

            StartRevealActions();

            if (revealGroup != null)
            {
                yield return revealGroup.FadeIn(revealFadeDuration);
            }

            if (delayAfterReveal > 0f)
            {
                yield return new WaitForSeconds(delayAfterReveal);
            }

            yield return PlayDialogueSegment(
                openingLineCount + dialogueLinesBeforeReveal,
                dialogueLinesAfterRevealBeforeAccuserMove);

            if (accuserMovementAfterRevealDialogue != null)
            {
                if (accuserMoveTarget != null)
                {
                    accuserMovementAfterRevealDialogue.MoveTo(accuserMoveTarget);
                }
                else
                {
                    accuserMovementAfterRevealDialogue.MoveNow();
                }

                while (accuserMovementAfterRevealDialogue.IsMoving)
                {
                    yield return null;
                }

                yield return WaitAfterAnimationDelay();
            }

            yield return PlayDialogueSegment(
                openingLineCount + dialogueLinesBeforeReveal + dialogueLinesAfterRevealBeforeAccuserMove,
                -1);

            if (delayBeforeEndingFadeOut > 0f)
            {
                yield return new WaitForSeconds(delayBeforeEndingFadeOut);
            }

            if (endingFadeOut != null)
            {
                yield return endingFadeOut.FadeOut();
            }

            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            UnlockPlayerControls();
        }

        private void StartRevealActions()
        {
            if (doubleTurnRoot != null)
            {
                doubleTurnRoot.localRotation = Quaternion.Euler(doubleTurnEuler);
            }

            if (guardMovementDuringReveal == null)
            {
                return;
            }

            guardMovementDuringReveal.MoveSpeed *= guardReturnSpeedMultiplier;
            guardMovementDuringReveal.InvertFlipDirection = guardReturnInvertFlipDirection;

            if (guardReturnTarget != null)
            {
                guardMovementDuringReveal.MoveTo(guardReturnTarget);
            }
            else
            {
                guardMovementDuringReveal.MoveNow();
            }
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

        private IEnumerator PlayAnimationState()
        {
            if (animator == null || string.IsNullOrWhiteSpace(animationStateName))
            {
                if (animationFallbackWait > 0f)
                {
                    yield return new WaitForSeconds(animationFallbackWait);
                }

                yield return WaitAfterAnimationDelay();

                yield break;
            }

            int stateHash = Animator.StringToHash(animationStateName);
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogWarning($"Animator does not have state '{animationStateName}'.", animator);
                yield break;
            }

            animator.speed = 1f;
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);

            yield return null;

            while (true)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != stateHash || stateInfo.normalizedTime >= 1f)
                {
                    yield return WaitAfterAnimationDelay();
                    yield break;
                }

                yield return null;
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
