using System.Collections.Generic;
using System.Collections;
using System;
using System.Text.RegularExpressions;
using Metacraft.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Metacraft.Dialogue
{
    public sealed class ClickDialoguePresenter : MonoBehaviour
    {
        [SerializeField] private TextAsset yarnScript;
        [SerializeField] private string startNode = "Scene01_Dialogue";
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0f)] private float playOnStartDelay;
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject avatarRoot;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Sprite edmundAvatar;
        [SerializeField] private Sprite npcAvatar;
        [SerializeField] private bool lockPlayerControlsWhilePlaying = true;
        [SerializeField] private SimplePlayerMovement2D playerMovement;
        [SerializeField] private string playerObjectName = "MainPlayer";

        private readonly List<DialogueLine> lines = new();
        private int currentLineIndex = -1;
        private int endLineIndex;
        private Coroutine typewriterRoutine;
        private string currentFullText = string.Empty;
        private bool isTyping;
        private Action onDialogueComplete;
        private bool controlsLockedByDialogue;

        private void OnDisable()
        {
            UnlockPlayerControls();
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            HideDialogue();

            if (playOnStart)
            {
                StartCoroutine(PlayAfterDelay());
            }
        }

        private void Update()
        {
            if (dialogueRoot == null || !dialogueRoot.activeSelf)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            bool proceed =
                (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame));

            if (proceed)
            {
                Advance();
            }
        }

        public void Play(string nodeName)
        {
            Play(nodeName, 0, -1, null);
        }

        public void Play(string nodeName, int startLineIndex, int maxLines, Action completed = null)
        {
            if (!HasRequiredReferences())
            {
                completed?.Invoke();
                return;
            }

            lines.Clear();
            lines.AddRange(ParseNode(nodeName));

            if (lines.Count == 0)
            {
                Debug.LogWarning($"No dialogue lines found for Yarn node '{nodeName}'.", this);
                HideDialogue();
                completed?.Invoke();
                return;
            }

            int clampedStartIndex = Mathf.Clamp(startLineIndex, 0, lines.Count);
            currentLineIndex = clampedStartIndex - 1;
            endLineIndex = maxLines > 0
                ? Mathf.Min(clampedStartIndex + maxLines, lines.Count)
                : lines.Count;
            onDialogueComplete = completed;

            ShowDialogue();
            ShowNextLine();
        }

        private IEnumerator PlayAfterDelay()
        {
            if (playOnStartDelay > 0f)
            {
                yield return new WaitForSeconds(playOnStartDelay);
            }

            Play(startNode, 0, -1);
        }

        private void ShowNextLine()
        {
            StopTypewriter();
            currentLineIndex++;

            if (currentLineIndex >= endLineIndex)
            {
                Action completed = onDialogueComplete;
                onDialogueComplete = null;
                HideDialogue();
                completed?.Invoke();
                return;
            }

            DialogueLine line = lines[currentLineIndex];
            speakerText.gameObject.SetActive(line.HasSpeaker);
            speakerText.text = line.Speaker;
            UpdateAvatar(line);
            currentFullText = line.Text;
            bodyText.gameObject.SetActive(true);
            typewriterRoutine = StartCoroutine(TypeLine(currentFullText));
        }

        private void Advance()
        {
            if (isTyping)
            {
                StopTypewriter();
                bodyText.text = currentFullText;
                return;
            }

            ShowNextLine();
        }

        private IEnumerator TypeLine(string text)
        {
            isTyping = true;
            bodyText.text = string.Empty;

            float delay = 1f / charactersPerSecond;
            for (int i = 0; i < text.Length; i++)
            {
                bodyText.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            typewriterRoutine = null;
        }

        private void StopTypewriter()
        {
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }

            isTyping = false;
        }

        private void ShowDialogue()
        {
            LockPlayerControls();
            dialogueRoot.SetActive(true);
            bodyText.gameObject.SetActive(true);
        }

        private void HideDialogue()
        {
            StopTypewriter();
            currentFullText = string.Empty;

            if (speakerText != null)
            {
                speakerText.text = string.Empty;
                speakerText.gameObject.SetActive(false);
            }

            if (bodyText != null)
            {
                bodyText.text = string.Empty;
                bodyText.gameObject.SetActive(false);
            }

            HideAvatar();

            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(false);
            }

            UnlockPlayerControls();
        }

        private void UpdateAvatar(DialogueLine line)
        {
            if (avatarImage == null)
            {
                return;
            }

            if (!line.HasSpeaker)
            {
                HideAvatar();
                return;
            }

            Sprite avatar = string.Equals(line.Speaker, "Edmund", StringComparison.OrdinalIgnoreCase)
                ? edmundAvatar
                : npcAvatar;

            if (avatar == null)
            {
                HideAvatar();
                return;
            }

            avatarImage.sprite = avatar;
            avatarImage.preserveAspect = true;
            if (avatarRoot != null)
            {
                avatarRoot.SetActive(true);
            }

            avatarImage.gameObject.SetActive(true);
        }

        private void HideAvatar()
        {
            if (avatarRoot != null)
            {
                avatarRoot.SetActive(false);
                return;
            }

            if (avatarImage != null)
            {
                avatarImage.gameObject.SetActive(false);
            }
        }

        private void LockPlayerControls()
        {
            if (!lockPlayerControlsWhilePlaying || controlsLockedByDialogue)
            {
                return;
            }

            ResolvePlayerMovement();
            if (playerMovement == null)
            {
                return;
            }

            playerMovement.SetControlsLocked(true);
            controlsLockedByDialogue = true;
        }

        private void UnlockPlayerControls()
        {
            if (!controlsLockedByDialogue || playerMovement == null)
            {
                return;
            }

            playerMovement.SetControlsLocked(false);
            controlsLockedByDialogue = false;
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

        private IEnumerable<DialogueLine> ParseNode(string nodeName)
        {
            string sourceText = yarnScript != null ? yarnScript.text : string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                yield break;
            }

            string[] rawLines = sourceText.Split('\n');
            bool inTargetNode = false;
            bool inBody = false;
            Regex dialoguePattern = new Regex(@"^\s*([^:<>-][^:]*):\s*(.+)\s*$");

            foreach (string rawLine in rawLines)
            {
                string line = rawLine.Trim();

                if (line.StartsWith("title:"))
                {
                    string title = line.Substring("title:".Length).Trim();
                    inTargetNode = title == nodeName;
                    inBody = false;
                    continue;
                }

                if (!inTargetNode)
                {
                    continue;
                }

                if (line == "---")
                {
                    inBody = true;
                    continue;
                }

                if (line == "===")
                {
                    yield break;
                }

                if (!inBody || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                Match match = dialoguePattern.Match(line);
                if (match.Success)
                {
                    string speaker = match.Groups[1].Value.Trim();
                    string text = match.Groups[2].Value.Trim();
                    bool hasSpeaker = !string.Equals(speaker, "Narrator", System.StringComparison.OrdinalIgnoreCase);
                    yield return new DialogueLine(hasSpeaker ? speaker : string.Empty, text, hasSpeaker);
                    continue;
                }

                yield return new DialogueLine(string.Empty, line, false);
            }
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = dialogueRoot != null && speakerText != null && bodyText != null;
            if (!hasReferences)
            {
                Debug.LogWarning("ClickDialoguePresenter needs Dialogue Root, Speaker Text, and Body Text references assigned in the Inspector.", this);
            }

            return hasReferences;
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string text, bool hasSpeaker)
            {
                Speaker = speaker;
                Text = text;
                HasSpeaker = hasSpeaker;
            }

            public string Speaker { get; }
            public string Text { get; }
            public bool HasSpeaker { get; }
        }
    }
}
