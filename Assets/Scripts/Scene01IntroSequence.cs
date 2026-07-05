using System.Collections;
using Metacraft.Dialogue;
using Metacraft.NPC;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [DefaultExecutionOrder(1000)]
    public sealed class Scene01IntroSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene01_Dialogue";
        [SerializeField, Min(0f)] private float startDelay = 2.2f;
        [SerializeField, Min(1)] private int openingLineCount = 1;
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private string characterObjectName = "MainPlayer";
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";
        [SerializeField] private string wakeStateName = "HospitalWake";
        [SerializeField, Min(0f)] private float delayAfterAnimation = 0.5f;
        [SerializeField, Min(0f)] private float afterWakeDelay = 1f;
        [SerializeField] private NpcMoveToPoint2D doctorMovement;
        [SerializeField] private string followUpStateName;
        [SerializeField, Min(0f)] private float followUpFallbackWait;

        private bool dialogueCompleted;
        private PlayerAnimationDriver2D animationDriver;
        private bool controlsLockedBySequence;

        private void Awake()
        {
            LockPlayerControls();
            PrepareCharacterForIntro();
        }

        private void OnDisable()
        {
            UnlockPlayerControls();
        }

        private IEnumerator Start()
        {
            if (dialoguePresenter == null)
            {
                Debug.LogWarning("Scene01IntroSequence needs a Dialogue Presenter reference.", this);
                UnlockPlayerControls();
                yield break;
            }

            LockPlayerControls();
            PrepareCharacterForIntro();

            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            yield return PlayDialogueSegment(0, openingLineCount);

            yield return PlayAnimationState(wakeStateName, 0f);

            if (afterWakeDelay > 0f)
            {
                yield return new WaitForSeconds(afterWakeDelay);
            }

            if (doctorMovement != null)
            {
                doctorMovement.MoveNow();
                while (doctorMovement.IsMoving)
                {
                    yield return null;
                }

                yield return WaitAfterAnimationDelay();
            }

            yield return PlayAnimationState(followUpStateName, followUpFallbackWait);

            yield return PlayDialogueSegment(openingLineCount, -1);

            if (animationDriver != null)
            {
                animationDriver.enabled = true;
            }

            UnlockPlayerControls();
        }

        private void PrepareCharacterForIntro()
        {
            ResolveCharacterAnimator();

            if (characterAnimator == null)
            {
                return;
            }

            animationDriver = characterAnimator.GetComponent<PlayerAnimationDriver2D>();
            if (animationDriver != null)
            {
                animationDriver.enabled = false;
            }

            int wakeStateHash = Animator.StringToHash(wakeStateName);
            if (!string.IsNullOrWhiteSpace(wakeStateName) && characterAnimator.HasState(0, wakeStateHash))
            {
                characterAnimator.Play(wakeStateHash, 0, 0f);
                characterAnimator.Update(0f);
            }

            characterAnimator.speed = 0f;
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

        private void ResolveCharacterAnimator()
        {
            if (characterAnimator != null || string.IsNullOrWhiteSpace(characterObjectName))
            {
                return;
            }

            GameObject characterObject = GameObject.Find(characterObjectName);
            if (characterObject != null)
            {
                characterAnimator = characterObject.GetComponentInChildren<Animator>();
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

        private IEnumerator PlayAnimationState(string stateName, float fallbackWait)
        {
            if (characterAnimator == null || string.IsNullOrWhiteSpace(stateName))
            {
                if (fallbackWait > 0f)
                {
                    yield return new WaitForSeconds(fallbackWait);
                }

                yield return WaitAfterAnimationDelay();

                yield break;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!characterAnimator.HasState(0, stateHash))
            {
                Debug.LogWarning($"Animator does not have state '{stateName}'.", characterAnimator);
                yield break;
            }

            characterAnimator.speed = 1f;
            characterAnimator.Play(stateHash, 0, 0f);
            characterAnimator.Update(0f);

            yield return null;

            while (true)
            {
                AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
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
    }
}
