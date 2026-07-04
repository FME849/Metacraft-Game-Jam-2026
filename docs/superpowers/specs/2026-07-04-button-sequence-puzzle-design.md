---
# Button Sequence Puzzle Design

**Date:** 2026-07-04
**Branch:** fme849/button-sequence-puzzle

## Overview

A 3-button, 3-light sequence puzzle in the style of Diablo 2's statue puzzle. The player must press three buttons in a fixed, designer-set order. Each correct press lights its indicator; a press out of order turns all indicators off and resets progress to the start. The puzzle stays open on a wrong press — only pressing the full correct sequence completes it.

This puzzle is a new concrete puzzle type built entirely on the existing puzzle system described in `2026-07-03-puzzle-system-design.md` (`PuzzleController`, `PuzzleManager`, `PuzzleResultChannelSO`, interactable-triggered UI modal, `Time.timeScale = 0` pause). No changes to that shared system are needed.

## Requirements

- 3 UI buttons, 3 UI light indicators (one indicator per button, same index).
- Correct order is a fixed sequence of button indices, set in the Inspector per puzzle instance (not randomized, not player-defined).
- Player interacts by clicking buttons directly with the mouse (no separate "walk up and press E" step — same modal-UI style as the clock puzzle).
- Correct press (matches the next expected index): turn that button's light on, advance progress.
- All three correct in order: puzzle is solved — raise `PuzzleResult.Success`.
- Incorrect press (does not match the next expected index): turn all lights off, reset progress to zero. Puzzle remains open; player retries from the start. No extra feedback (no flash/sound) beyond lights going off.
- Escape key cancels the puzzle (`PuzzleResult.Cancelled`), matching `ClockPuzzleController`'s existing behavior.
- Opened via the existing generic `ChestInteractable` (already accepts any `PuzzleController` prefab) — no new interactable script needed.

## Components

### `ButtonSequencePuzzleController : PuzzleController`

`Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequencePuzzleController.cs`

Serialized fields:
- `Button[] buttons` — the 3 clickable UI buttons, wired in the Inspector.
- `Image[] lightIndicators` — the 3 light images, index-aligned with `buttons`.
- `int[] correctSequence` — the fixed correct order, expressed as indices into `buttons` (e.g. `{1, 2, 0}` means press button 2, then button 3, then button 1). Set per puzzle instance in the Inspector.
- `Color lightOnColor`, `Color lightOffColor` — same pattern as `ClockPuzzleController`.

Runtime state:
- `int _progressIndex` — count of correct presses made in a row (0..`correctSequence.Length`).

Behavior:
- `Awake()`: for each `buttons[i]`, register `buttons[i].onClick.AddListener(() => OnButtonPressed(i))`.
- `OnButtonPressed(int index)`:
  - If `index == correctSequence[_progressIndex]`: set `lightIndicators[index].color = lightOnColor`; increment `_progressIndex`. If `_progressIndex == correctSequence.Length`, call `Complete(PuzzleResult.Success)`.
  - Else: set every `lightIndicators[*].color = lightOffColor`; reset `_progressIndex = 0`.
- `Update()`: if `Keyboard.current?.escapeKey.wasPressedThisFrame == true`, call `Complete(PuzzleResult.Cancelled)`.

### Reused, unchanged

- `PuzzleController`, `PuzzleManager`, `PuzzleResultChannelSO`, `PuzzleResult` — no modifications.
- `ChestInteractable` — reused as-is; a new instance in the scene references the `ButtonSequencePuzzle` prefab via its existing `PuzzleController` field.

## Key Decisions

- **No new interactable script.** `ChestInteractable`'s prefab field is typed `PuzzleController`, so it already works with any puzzle type.
- **Wrong press does not raise a result.** `PuzzleResult.Fail` is not used by this puzzle — a wrong press is an in-puzzle reset, not a puzzle-ending event. Only `Success` (full correct sequence) and `Cancelled` (Escape) end the puzzle, matching `ClockPuzzleController`'s existing use of the enum.
- **Sequence is fixed data, not generated.** `correctSequence` is designer-authored per puzzle instance in the Inspector, consistent with how `ClockPuzzleController` hardcodes `targetHour`/`targetMinute`.
- **Lights are `UnityEngine.UI.Image` color swaps**, matching `ClockPuzzleController`'s `lightIndicator` — no `Light` component, `Renderer`, or `Animator`.
- **Button click wiring is done in code (`Awake`)**, not per-button helper scripts, since a closure-captured index is sufficient — avoids introducing an unnecessary new component type.

## File Structure

```
Assets/
  Scripts/
    Puzzle/
      ButtonSequencePuzzle/
        ButtonSequencePuzzleController.cs
  Prefabs/
    Puzzle/
      Button Sequence Puzzle.prefab
```
