# Fade-In SFX Cue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let `SceneSpriteGroupFade2D.FadeIn()` optionally trigger a sound effect the moment it starts, with independent control over how long that SFX plays (fade-out cutoff instead of always playing the full clip).

**Architecture:** A new standalone `MonoBehaviour`, `TimedSfxCue`, owns one `AudioSource` (via `RequireComponent`) and exposes a single `Play()` method — it plays a configured `AudioClip` and, if `maxDuration > 0`, fades the volume to zero over `fadeOutDuration` seconds before stopping and resetting for the next `Play()` call. `SceneSpriteGroupFade2D` gets one new optional field, `fadeInSfx` (type `TimedSfxCue`), and calls `fadeInSfx?.Play()` as the very first line of `FadeIn()`. Each SFX cue lives on its own dedicated GameObject in the scene, so cues never share or fight over an `AudioSource`.

**Tech Stack:** Unity, C#, `UnityEngine.AudioSource`, coroutines (`IEnumerator`/`StartCoroutine`).

## Global Constraints

- `fadeInSfx` is optional — when unset, `FadeIn()` behavior is unchanged (must not affect the two existing `SceneSpriteGroupFade2D` instances in `Scene03.unity`, which will not get a cue wired up) (per spec).
- `TimedSfxCue` requires its own `AudioSource` on the same GameObject via `[RequireComponent(typeof(AudioSource))]` — no shared-source or temporary-GameObject fallback (per spec).
- `maxDuration = 0` (the default) means play the full clip length, no cutoff (per spec).
- Cutting a cue short is always a fade-out over `fadeOutDuration` seconds, never a hard `Stop()` (per spec).
- Calling `Play()` again while a previous fade-out is still in progress must stop the old fade coroutine and restore volume before starting again — no overlapping fades, no volume stuck at 0 (per spec).
- The SFX cue fires unconditionally on the first line of `FadeIn(float duration)`, before `CacheRenderers()`, regardless of `duration` (including the `duration <= 0` instant-show path) (per spec).
- Reference spec: `docs/superpowers/specs/2026-07-05-fade-in-sfx-cue-design.md`.

---

### Task 1: `TimedSfxCue` — standalone SFX cue component

**Files:**
- Create: `Assets/Scripts/TimedSfxCue.cs`

**Interfaces:**
- Produces: `Metacraft.SceneFlow.TimedSfxCue`, a `[RequireComponent(typeof(AudioSource))]` `MonoBehaviour` with serialized fields `AudioClip clip`, `float maxDuration` (`[Min(0f)]`, default `0f`), `float fadeOutDuration` (`[Min(0f)]`, default `0.3f`), and public method `void Play()`. Task 2 adds a field of this type to `SceneSpriteGroupFade2D` and calls `Play()` on it.

No automated test for this task: it depends on live `AudioSource` playback and coroutine timing, the same situation as `PipePuzzleSolvedSequence` and `MusicFadeIn` (no automated test, manual Play Mode verification only). Task 3 covers that manual verification.

- [ ] **Step 1: Write the component**

Create `Assets/Scripts/TimedSfxCue.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace Metacraft.SceneFlow
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class TimedSfxCue : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(0f)] private float maxDuration;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

        private AudioSource audioSource;
        private float originalVolume;
        private Coroutine fadeOutCoroutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            originalVolume = audioSource.volume;
        }

        public void Play()
        {
            if (clip == null)
            {
                return;
            }

            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = null;
                audioSource.volume = originalVolume;
            }

            audioSource.clip = clip;
            audioSource.Play();

            if (maxDuration > 0f)
            {
                fadeOutCoroutine = StartCoroutine(StopAfterDuration());
            }
        }

        private IEnumerator StopAfterDuration()
        {
            float waitTime = Mathf.Max(0f, maxDuration - fadeOutDuration);
            yield return new WaitForSeconds(waitTime);

            float startVolume = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = originalVolume;
            fadeOutCoroutine = null;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Open the project in the Unity Editor and let it finish recompiling.
Expected: Console shows no compile errors, and a `Assets/Scripts/TimedSfxCue.cs.meta` file has been generated next to the new script.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TimedSfxCue.cs Assets/Scripts/TimedSfxCue.cs.meta
git commit -m "feat: add TimedSfxCue component for fade-out-controlled SFX playback"
```

---

### Task 2: Wire `fadeInSfx` into `SceneSpriteGroupFade2D`

**Files:**
- Modify: `Assets/Scripts/SceneSpriteGroupFade2D.cs:9-11` (field declarations)
- Modify: `Assets/Scripts/SceneSpriteGroupFade2D.cs:27-38` (`FadeIn` method)

**Interfaces:**
- Consumes: `Metacraft.SceneFlow.TimedSfxCue.Play()` (Task 1).
- Produces: new serialized field `fadeInSfx` on `Metacraft.SceneFlow.SceneSpriteGroupFade2D`. Task 3 wires this field to a `TimedSfxCue` GameObject in `Scene02.unity` via the Inspector.

No automated test for this task, consistent with `SceneSpriteGroupFade2D`'s existing untested `FadeIn` coroutine (no EditMode/PlayMode test exists for it today). Task 3 covers manual verification.

- [ ] **Step 1: Add the `fadeInSfx` field**

In `Assets/Scripts/SceneSpriteGroupFade2D.cs`, the field block currently reads:

```csharp
        [SerializeField] private Transform[] targets;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField, Min(0f)] private float eyesFadeDelay = 1f;
```

Change it to:

```csharp
        [SerializeField] private Transform[] targets;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField, Min(0f)] private float eyesFadeDelay = 1f;
        [SerializeField] private TimedSfxCue fadeInSfx;
```

- [ ] **Step 2: Trigger the cue at the start of `FadeIn`**

In the same file, `FadeIn` currently starts with:

```csharp
        public IEnumerator FadeIn(float duration)
        {
            if (renderers.Count == 0)
            {
                CacheRenderers();
            }
```

Change it to:

```csharp
        public IEnumerator FadeIn(float duration)
        {
            fadeInSfx?.Play();

            if (renderers.Count == 0)
            {
                CacheRenderers();
            }
```

- [ ] **Step 3: Verify it compiles**

Open the project in the Unity Editor and let it finish recompiling.
Expected: Console shows no compile errors. Open `Assets/Scenes/Scene03.unity` in the editor and confirm the two existing `SceneSpriteGroupFade2D` components there show a new empty `Fade In Sfx` field in the Inspector (unset — no behavior change).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/SceneSpriteGroupFade2D.cs
git commit -m "feat: trigger optional SFX cue at the start of SceneSpriteGroupFade2D.FadeIn"
```

---

### Task 3: Wire a cue into Scene02's reveal fade and verify (manual, Unity Editor)

**Files:**
- Modify: `Assets/Scenes/Scene02.unity`

**Interfaces:**
- Consumes: `Metacraft.SceneFlow.TimedSfxCue` (Task 1) Inspector fields (`Clip`, `Max Duration`, `Fade Out Duration`); `Metacraft.SceneFlow.SceneSpriteGroupFade2D.fadeInSfx` (Task 2), on the `revealGroup` referenced by `SceneDialogueTriggerSequence` in Scene02.

This task is GUI work in the Unity Editor and cannot be expressed as shell commands.

- [ ] **Step 1: Create the cue GameObject**

In `Scene02.unity`:
1. Create a new empty GameObject named `SFX_RevealFadeIn`.
2. Add an `Audio Source` component. Uncheck `Play On Awake` and `Loop`.
3. Add the `Timed Sfx Cue` component (from Task 1).
4. Set `Clip` to an available reveal-appropriate clip, e.g. `Assets/Audio/GameJamScene02.wav`.
5. Set `Max Duration` to `2` and `Fade Out Duration` to `0.3` as a starting point (adjust by ear later — both are Inspector-only, no code changes needed to retune).

- [ ] **Step 2: Wire it to the reveal fade**

1. Find the GameObject holding the `Scene Sprite Group Fade 2D` component that `SceneDialogueTriggerSequence`'s `Reveal Group` field points to.
2. In its Inspector, drag `SFX_RevealFadeIn` into the new `Fade In Sfx` field.

- [ ] **Step 3: Manual verification (Play Mode)**

1. Enter Play Mode in `Scene02.unity` and walk through the dialogue trigger sequence up to the reveal step (or temporarily call `revealGroup.FadeIn()` / trigger the sequence per existing test flow used for this scene).
2. Confirm the SFX starts playing at the exact moment the reveal sprites begin fading in.
3. Confirm the SFX fades out and stops around the 2-second mark instead of playing the full clip (assuming the clip is longer than 2s; if not, pick a longer clip or lower `Max Duration` to something the clip is longer than, to actually observe the cutoff).
4. Trigger the reveal a second time in the same Play Mode session (re-enter Play Mode, or re-trigger if the sequence allows it) and confirm the SFX plays at full volume again — not stuck at a faded-out volume from the previous run.

- [ ] **Step 4: Regression check Scene03**

1. Open `Assets/Scenes/Scene03.unity` and enter Play Mode.
2. Confirm both existing `SceneSpriteGroupFade2D` instances still fade in exactly as before, with no console errors or missing-reference warnings — their `Fade In Sfx` field is intentionally left unset, so no SFX should play.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Scene02.unity
git commit -m "feat: wire fade-in SFX cue into Scene02 reveal sequence"
```
