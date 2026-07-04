using System.Collections;
using Metacraft.Dialogue;
using Metacraft.Player;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    public sealed class Scene05IntroSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene05_Dialogue";
        [SerializeField, Min(1)] private int linesBeforeFade = 12;
        [SerializeField] private int linesAfterFade = 3;
        [SerializeField] private SceneFadeIn fadeFromBlack;
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
                UnlockPlayerControls();
                yield break;
            }

            yield return PlayDialogueSegment(0, linesBeforeFade);

            if (fadeFromBlack != null)
            {
                yield return fadeFromBlack.FadeIn();
            }

            if (linesAfterFade != 0)
            {
                yield return PlayDialogueSegment(linesBeforeFade, linesAfterFade);
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
