using System.Collections;
using Metacraft.Dialogue;
using Metacraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Metacraft.SceneFlow
{
    public sealed class ScenePortal2D : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "Scene02";
        [SerializeField] private string playerObjectName = "MainPlayer";
        [SerializeField] private string playerTag;
        [SerializeField] private SceneFadeOut fadeOut;
        [SerializeField] private ClickDialoguePresenter dialoguePresenter;
        [SerializeField] private string dialogueNodeName;
        [SerializeField, Min(0)] private int dialogueStartLineIndex;
        [SerializeField] private int dialogueLineCount = -1;
        [SerializeField] private RectTransform pageImage;
        [SerializeField] private Vector3 pageImageLocalPosition = Vector3.zero;
        [SerializeField] private bool waitForClickAfterPageImage;
        [SerializeField] private bool hidePageImageAfterClick = true;
        [SerializeField] private int afterPageDialogueLineCount = -1;
        [SerializeField, Min(0f)] private float delayAfterSequence;
        [SerializeField] private bool loadSceneAfterSequence = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;

        private bool loading;
        private bool dialogueCompleted;
        private bool controlsLockedBySequence;
        private bool hasFadedOut;

        private void OnDisable()
        {
            UnlockPlayerControls();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (loading || !IsPlayer(other))
            {
                return;
            }

            loading = true;
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            LockPlayerControls();

            if (fadeOut == null)
            {
                fadeOut = FindSceneFadeOut();
            }

            if (fadeOut != null)
            {
                yield return fadeOut.FadeOut();
                hasFadedOut = true;
            }

            if (dialoguePresenter != null && !string.IsNullOrWhiteSpace(dialogueNodeName))
            {
                yield return PlayDialogueSegment(dialogueStartLineIndex, dialogueLineCount);
            }

            if (pageImage != null)
            {
                pageImage.gameObject.SetActive(true);
                pageImage.localPosition = pageImageLocalPosition;
                pageImage.anchoredPosition3D = pageImageLocalPosition;

                if (waitForClickAfterPageImage)
                {
                    yield return WaitForProceedInput();
                }

                if (hidePageImageAfterClick)
                {
                    pageImage.gameObject.SetActive(false);
                }
            }

            if (afterPageDialogueLineCount != 0 &&
                dialoguePresenter != null &&
                !string.IsNullOrWhiteSpace(dialogueNodeName) &&
                dialogueLineCount > 0)
            {
                yield return PlayDialogueSegment(
                    dialogueStartLineIndex + dialogueLineCount,
                    afterPageDialogueLineCount);
            }

            if (delayAfterSequence > 0f)
            {
                yield return new WaitForSeconds(delayAfterSequence);
            }

            if (loadSceneAfterSequence)
            {
                yield return LoadSceneAfterFade();
            }
            else
            {
                UnlockPlayerControls();
            }
        }

        private IEnumerator PlayDialogueSegment(int startLineIndex, int lineCount)
        {
            dialogueCompleted = false;
            dialoguePresenter.Play(
                dialogueNodeName,
                startLineIndex,
                lineCount,
                () => dialogueCompleted = true);

            while (!dialogueCompleted)
            {
                yield return null;
            }
        }

        private IEnumerator LoadSceneAfterFade()
        {
            if (fadeOut == null)
            {
                fadeOut = FindSceneFadeOut();
            }

            if (fadeOut != null && !hasFadedOut)
            {
                yield return fadeOut.FadeOut();
                hasFadedOut = true;
            }

            SceneManager.LoadScene(targetSceneName);
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

        private bool IsPlayer(Collider2D other)
        {
            if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(playerObjectName))
            {
                return false;
            }

            return other.name == playerObjectName ||
                other.transform.root.name == playerObjectName ||
                other.GetComponentInParent<Transform>().root.name == playerObjectName;
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

        private static SceneFadeOut FindSceneFadeOut()
        {
            SceneFadeOut[] candidates = Resources.FindObjectsOfTypeAll<SceneFadeOut>();
            for (int i = 0; i < candidates.Length; i++)
            {
                SceneFadeOut candidate = candidates[i];
                if (candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
