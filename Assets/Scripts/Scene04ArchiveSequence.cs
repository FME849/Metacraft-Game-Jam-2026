using System.Collections;
using Metacraft.Dialogue;
using Metacraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metacraft.SceneFlow
{
    public sealed class Scene04ArchiveSequence : MonoBehaviour
    {
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string nodeName = "Scene04_Dialogue";
        [SerializeField, Min(0)] private int beforePageLineIndex;
        [SerializeField, Min(1)] private int beforePageLineCount = 7;
        [SerializeField] private RectTransform pageImage;
        [SerializeField] private Vector3 pageImageLocalPosition = Vector3.zero;
        [SerializeField] private bool hidePageImageAfterClick = true;
        [SerializeField, Min(0)] private int afterPageLineCount = 15;
        [SerializeField, Min(0f)] private float delayAfterDialogue = 1.5f;
        [SerializeField] private int lineCountAfterDelay = -1;
        [SerializeField] private SceneFadeIn fadeInAfterDelay;
        [SerializeField] private int lineCountAfterFade = -1;
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

            yield return PlayDialogueSegment(beforePageLineIndex, beforePageLineCount);

            if (pageImage != null)
            {
                pageImage.gameObject.SetActive(true);
                pageImage.localPosition = pageImageLocalPosition;
                pageImage.anchoredPosition3D = pageImageLocalPosition;

                yield return WaitForProceedInput();

                if (hidePageImageAfterClick)
                {
                    pageImage.gameObject.SetActive(false);
                }
            }

            if (afterPageLineCount > 0)
            {
                yield return PlayDialogueSegment(beforePageLineIndex + beforePageLineCount, afterPageLineCount);
            }

            if (delayAfterDialogue > 0f)
            {
                yield return new WaitForSeconds(delayAfterDialogue);
            }

            if (lineCountAfterDelay != 0)
            {
                yield return PlayDialogueSegment(
                    beforePageLineIndex + beforePageLineCount + afterPageLineCount,
                    lineCountAfterDelay);
            }

            if (fadeInAfterDelay != null)
            {
                yield return fadeInAfterDelay.FadeIn();
            }

            if (lineCountAfterFade != 0)
            {
                yield return PlayDialogueSegment(
                    beforePageLineIndex + beforePageLineCount + afterPageLineCount + Mathf.Max(0, lineCountAfterDelay),
                    lineCountAfterFade);
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

        private static IEnumerator WaitForProceedInput()
        {
            yield return null;

            while (true)
            {
                Mouse mouse = Mouse.current;
                Keyboard keyboard = Keyboard.current;

                bool proceed =
                    (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                    (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame));

                if (proceed)
                {
                    yield break;
                }

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
