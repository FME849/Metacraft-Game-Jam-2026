# Puzzle System Design

**Date:** 2026-07-03
**Branch:** fme849/gear-puzzle

## Overview

A reusable mini-puzzle system that can be embedded in the main game as instantiated prefabs. Uses a ScriptableObject event channel (observer pattern) to fully decouple puzzle logic from main gameplay. The first implementation is a gear puzzle; the architecture supports any future puzzle type.

## Requirements

- Puzzle opens as an instantiated prefab, overlaid on main scene
- Main game pauses (`Time.timeScale = 0`) while puzzle is active
- Player can cancel the puzzle at any time
- Result is one of three states: `Success`, `Fail`, `Cancelled`
- No result data beyond the status enum is needed
- On `Success`: interactable that triggered the puzzle disables itself (no repeat interaction)
- On `Fail` or `Cancelled`: puzzle resets on next open (prefab is stateless — Instantiate gives a clean slate)
- Multiple puzzle types will share this flow

## Architecture

```
[Interactable]          [PuzzleManager]       [PuzzleResultChannelSO]    [Puzzle Prefab]
      │                        │                          │                      │
      │── Open(prefab) ───────►│                          │                      │
      │                        │── Instantiate ──────────────────────────────── │
      │                        │── timeScale = 0          │                      │
      │                        │── subscribe ────────────►│                      │
      │                        │                          │                      │
      │── subscribe ──────────────────────────────────────│                      │
      │                        │                          │◄── Raise(result) ────│
      │◄── OnRaised(result) ───────────────────────────── │                      │
      │                        │◄── OnRaised(result) ─────│                      │
      │                        │── Destroy prefab          │                      │
      │                        │── timeScale = 1           │                      │
```

## Components

### `PuzzleResult` (enum)

```csharp
public enum PuzzleResult { Success, Fail, Cancelled }
```

### `PuzzleResultChannelSO` (ScriptableObject)

The single shared wire between puzzle prefabs and any listener. One asset in Project, referenced via Inspector by both `PuzzleManager` and interactables.

```csharp
[CreateAssetMenu(menuName = "Puzzle/Result Channel")]
public class PuzzleResultChannelSO : ScriptableObject
{
    public event Action<PuzzleResult> OnRaised;
    public void Raise(PuzzleResult result) => OnRaised?.Invoke(result);
}
```

### `PuzzleController` (abstract MonoBehaviour — on every puzzle prefab)

Base class all puzzle prefabs inherit from. Subclasses call `Complete()` when the puzzle ends.

```csharp
public abstract class PuzzleController : MonoBehaviour
{
    [SerializeField] PuzzleResultChannelSO resultChannel;

    protected void Complete(PuzzleResult result)
        => resultChannel.Raise(result);

    public virtual void OnPuzzleOpened() { }
}
```

### `PuzzleManager` (MonoBehaviour singleton — in main scene)

Owns the lifecycle: instantiate prefab, pause, receive result, destroy, resume.

```csharp
public class PuzzleManager : MonoBehaviour
{
    [SerializeField] PuzzleResultChannelSO resultChannel;

    private PuzzleController _activePuzzle;

    public void Open(PuzzleController prefab)
    {
        _activePuzzle = Instantiate(prefab);
        Time.timeScale = 0f;
        _activePuzzle.OnPuzzleOpened();
        resultChannel.OnRaised += OnPuzzleCompleted;
    }

    private void OnPuzzleCompleted(PuzzleResult result)
    {
        resultChannel.OnRaised -= OnPuzzleCompleted;
        Destroy(_activePuzzle.gameObject);
        Time.timeScale = 1f;
        _activePuzzle = null;
    }
}
```

### `GearPuzzleController` (first concrete puzzle)

```csharp
public class GearPuzzleController : PuzzleController
{
    // gear-specific logic here
    // call Complete(PuzzleResult.Success) when solved
    // call Complete(PuzzleResult.Cancelled) when player exits
    // call Complete(PuzzleResult.Fail) if applicable
}
```

### Interactable (example)

```csharp
public class ChestInteractable : MonoBehaviour
{
    [SerializeField] PuzzleManager puzzleManager;
    [SerializeField] PuzzleController gearPuzzlePrefab;
    [SerializeField] PuzzleResultChannelSO resultChannel;

    void OnInteract()
    {
        resultChannel.OnRaised += HandleResult;
        puzzleManager.Open(gearPuzzlePrefab);
    }

    void HandleResult(PuzzleResult result)
    {
        resultChannel.OnRaised -= HandleResult;
        if (result == PuzzleResult.Success)
            gameObject.SetActive(false);
        // Fail or Cancelled: do nothing, puzzle resets on next open
    }
}
```

## Key Decisions

- **Puzzle prefab is stateless.** `Instantiate` always gives a clean slate; no manual reset needed.
- **No completed-state tracking.** Interactable disables itself on `Success` — player cannot interact again.
- **Both `PuzzleManager` and interactable unsubscribe immediately after receiving the event** to prevent memory leaks and double-fire on subsequent puzzles.
- **`Time.timeScale = 0` for pause.** Any puzzle animations must use `unscaledDeltaTime` or UI animations.

## File Structure

```
Assets/
  Scripts/
    Puzzle/
      PuzzleResult.cs
      PuzzleResultChannelSO.cs
      PuzzleController.cs
      PuzzleManager.cs
      GearPuzzle/
        GearPuzzleController.cs
  Prefabs/
    Puzzle/
      GearPuzzle.prefab
  ScriptableObjects/
    Puzzle/
      PuzzleResultChannel.asset
```
