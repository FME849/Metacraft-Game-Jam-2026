---
# Fade-In SFX Cue Design

**Date:** 2026-07-05
**Branch:** fme849/button-sequence-puzzle

## Overview

`SceneSpriteGroupFade2D` (used by `SceneDialogueTriggerSequence.revealGroup` in Scene02, and by two other instances in Scene03) currently has no way to play a sound effect when its `FadeIn()` coroutine starts. The request is to trigger an SFX at the start of `FadeIn()`, with control over how long the SFX plays (not necessarily the full clip) via a fade-out rather than a hard cut.

Audio playback is pulled out into a new standalone component, `TimedSfxCue`, rather than adding `AudioSource`/`AudioClip`/duration fields directly to `SceneSpriteGroupFade2D`. This keeps the fade script focused on visuals, matches the project's "one component, one job" style (e.g. `MusicFadeIn`, `SceneFadeOut`), and lets a single reusable cue type be wired into any trigger point later (dialogue lines, puzzle solved, etc.), not just fade-in — each cue lives on its own GameObject in the scene.

## Requirements

- When `SceneSpriteGroupFade2D.FadeIn()` starts, optionally play a configured SFX. Optional: if nothing is wired up, behavior is unchanged (safe for the existing Scene03 instances).
- The SFX's own duration must be independently controllable — able to cut it short with a fade-out instead of always playing the full clip length.
- Playing the same cue again while a previous fade-out is still in progress must not leave overlapping/glitchy audio state.
- Follows the project's existing audio-field conventions (`AudioSource` + `AudioClip`, seen in `PipePuzzleSolvedSequence` and `MusicFadeIn`).

## Components

### `TimedSfxCue.cs`

`Assets/Scripts/TimedSfxCue.cs`, namespace `Metacraft.SceneFlow` (consistent with `MusicFadeIn`, `SceneFadeIn`, `SceneFadeOut`, which are also audio/fade-related but live in this namespace).

`[RequireComponent(typeof(AudioSource))]` — each GameObject carrying this component owns its own `AudioSource`; no shared-source or temporary-GameObject handling is needed.

Serialized fields:
- `AudioClip clip` — the sound to play.
- `float maxDuration` (`[Min(0f)]`, default `0f`) — `0` means play the full clip length; a positive value caps playback to that many seconds.
- `float fadeOutDuration` (`[Min(0f)]`, default `0.3f`) — only used when `maxDuration > 0`; the volume ramp-down window immediately before cutoff.

Public API:
- `void Play()` — starts (or restarts) playback.

Runtime behavior:
- `Awake()`: cache `AudioSource` and its original `volume`.
- `Play()`:
  - If a fade-out coroutine from a previous `Play()` call is still running, stop it first and restore `audioSource.volume` to the cached original (prevents overlapping fades leaving volume stuck at 0 or mid-fade).
  - Set `audioSource.clip = clip`, call `audioSource.Play()`.
  - If `maxDuration <= 0`, stop here — clip plays out naturally, no coroutine needed.
  - If `maxDuration > 0`, start a coroutine: wait `Mathf.Max(0f, maxDuration - fadeOutDuration)` seconds, then linearly ramp `audioSource.volume` from its current value to `0` over `fadeOutDuration` seconds, then `audioSource.Stop()` and reset `audioSource.volume` to the cached original (so the next `Play()` starts at full volume again).
- If `clip == null`, `Play()` is a no-op (logs nothing — matches the silent-no-op style of optional fields elsewhere, e.g. `SceneDialogueTriggerSequence` skipping steps when a reference is unset).

### `SceneSpriteGroupFade2D.cs` (modified)

`Assets/Scripts/SceneSpriteGroupFade2D.cs`

- New field: `[SerializeField] private TimedSfxCue fadeInSfx;`
- First line of `FadeIn(float duration)`: `fadeInSfx?.Play();` — fires immediately when the coroutine starts, before `CacheRenderers()` and regardless of `duration` (including the `duration <= 0` instant-show path).

## Key Decisions

- **Separate `TimedSfxCue` component instead of fields on `SceneSpriteGroupFade2D`.** Keeps the fade script single-purpose and lets the same cue type be reused for other trigger points in the future without touching fade logic.
- **One GameObject per cue.** Matches how the project already scopes single-purpose behaviors to their own GameObject/component pair; avoids a shared `AudioSource` being stopped/fought over by multiple unrelated cues.
- **`RequireComponent(AudioSource)` instead of an optional field with a temporary-GameObject fallback** (unlike `PipePuzzleSolvedSequence.PlayOneShot2D`). Since each cue already gets its own dedicated GameObject, there's no case where an `AudioSource` isn't available — the fallback path that pattern needed doesn't apply here.
- **Fade-out only, no hard `Stop()` option.** Per explicit request — cutting audio abruptly reads as a bug; fading out over a short configurable window sounds intentional.
- **`maxDuration = 0` means "play full clip.**" Matches the existing default-off convention used for optional durations elsewhere in the codebase (e.g. `eyesFadeDelay`, `delayAfterAnimation` defaulting to values that mean "no extra behavior").
- **SFX fires at the very start of `FadeIn()`, unconditionally.** Confirmed as the desired trigger point; simpler than gating on `duration` or waiting for completion, and matches "trigger when fade-in starts."

## File Structure

```
Assets/
  Scripts/
    TimedSfxCue.cs
    SceneSpriteGroupFade2D.cs   (modified)
```
