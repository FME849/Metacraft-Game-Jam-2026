---
# Text Crawl Intro Design

**Date:** 2026-07-04
**Branch:** fme849/button-sequence-puzzle

## Overview

A black-screen, vertically-scrolling text sequence ("Star Wars crawl" style, flat — no 3D perspective/tilt) used for the game's opening intro. The game currently has no scene of this kind; the project mentions more than one upcoming scene that is "pure scrolling text," so the crawl is built as a reusable prefab rather than a bespoke intro-only script. It does not use Yarn Spinner or the existing `.yarn`/`ClickDialoguePresenter` dialogue pipeline — that pipeline is for branching, click-to-advance dialogue, while a crawl is one-way, uninterruptible narration with no player input.

## Requirements

- Full-screen black background with white text scrolling straight upward at a constant speed (px/second), no perspective/skew.
- Not skippable — the player must watch the full crawl; no input advances or cancels it.
- Text content authored per-scene as a plain `string[]` in the Inspector — no dependency on any parser or dialogue asset.
- Optional background music, played once from `Start()`.
- On completion (content has fully scrolled past the top of the viewport), automatically load a configured next scene by name.
- Reusable across multiple scenes: the visual setup (Canvas, background, text container, audio) and behavior are packaged as a single prefab. Each scene instance only overrides Inspector fields (`lines`, `scrollSpeed`, `nextSceneName`, `backgroundMusic`) — no per-scene script or layout work.
- First concrete use: a new `Intro.unity` scene using this prefab, transitioning to `Scene01` on completion. `Intro.unity` and `Scene01.unity` must be added to `EditorBuildSettings` (currently only `SampleScene` is present), with `Intro` first.

## Components

### Prefab `TextCrawlScreen`

`Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab`

Hierarchy:
- `Canvas` (Screen Space - Overlay)
  - `Background` — full-screen `Image`, black
  - `CrawlContent` — `RectTransform` holding a single `TMP_Text`, anchored bottom-center so it can be positioned below the viewport at start
- `AudioSource` (`playOnAwake = false`; triggered explicitly so a missing clip doesn't error)

### `TextCrawl.cs`

`Assets/Scripts/TextCrawl/TextCrawl.cs`, attached to the prefab root.

Serialized fields:
- `string[] lines` — content for this instance, joined with blank-line separators into `CrawlContent`'s `TMP_Text` on `Start()`.
- `float scrollSpeed` (`[Min(1f)]`) — px/second, constant for the whole block.
- `string nextSceneName` — scene to load via `SceneManager.LoadScene` when the crawl finishes.
- `AudioClip backgroundMusic` (optional) — if assigned, played on the `AudioSource` at `Start()`.
- Inspector references to `TMP_Text`, `CrawlContent` (`RectTransform`), `Canvas`/viewport `RectTransform`, and `AudioSource`, all wired within the prefab.

Runtime behavior:
- `Start()`: set `TMP_Text.text` from `lines`; force a layout rebuild to read `preferredHeight`; compute total scroll distance via `TextCrawlMath.ComputeScrollDistance(viewportHeight, contentHeight)`; position `CrawlContent` at its starting (off-screen bottom) position; play `backgroundMusic` if assigned.
- `Update()`: advance `CrawlContent.anchoredPosition.y` by `scrollSpeed * Time.deltaTime`; when accumulated movement reaches the precomputed scroll distance, load `nextSceneName` (if non-empty) via `SceneManager.LoadScene` and stop updating.
- Missing required references (`TMP_Text`, `CrawlContent`, `AudioSource`) → log a warning and disable the component, matching the `HasRequiredReferences` pattern used by `ClickDialoguePresenter`.

### `TextCrawlMath.cs`

`Assets/Scripts/TextCrawl/TextCrawlMath.cs` — static helper, no `MonoBehaviour` dependency:
- `float ComputeScrollDistance(float viewportHeight, float contentHeight)` → `viewportHeight + contentHeight` (distance from fully-below-viewport start to fully-above-viewport end).

Pulled out purely so the scroll-completion math is unit-testable, following the existing EditMode test pattern in `Assets/Tests/EditMode/Puzzle`.

## Key Decisions

- **Reusable prefab, not a one-off intro script.** Other planned scenes are also pure text-crawl screens; packaging Canvas + background + text + audio + behavior as one prefab means layout/style changes happen in one place, and each scene only overrides four fields.
- **No skip input.** Deliberate per requirements — this crawl is mandatory, unlike the click-to-advance dialogue system.
- **Flat vertical scroll, no 3D perspective.** Matches the "Star Wars style" request while staying in scope for a game jam — a tilted/receding crawl would need materially more Canvas/camera setup for a purely cosmetic difference.
- **Content is a flat `string[]`, independent of Yarn Spinner and `.yarn` files.** The existing `.yarn` + `ClickDialoguePresenter` pipeline models branching, speaker-attributed, click-to-advance dialogue; a crawl is one continuous uninterrupted block with no speakers or branching, so reusing that pipeline would add indirection without benefit.
- **Single fixed `scrollSpeed` for the whole block, not per-line durations.** Simpler to configure; the crawl has no need to pace individual lines differently.
- **`nextSceneName` is a per-instance string field, not hardcoded.** Required for the prefab to be reusable across scenes with different destinations.
- **Scroll-distance math extracted into `TextCrawlMath`** purely so it has an EditMode test, consistent with existing project testing conventions.

## File Structure

```
Assets/
  Scripts/
    TextCrawl/
      TextCrawl.cs
      TextCrawlMath.cs
  Prefabs/
    TextCrawl/
      TextCrawlScreen.prefab
  Scenes/
    Intro.unity
  Tests/
    EditMode/
      TextCrawl/
        TextCrawlMathTests.cs
```
