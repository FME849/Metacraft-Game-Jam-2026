# Button Sequence Puzzle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a 3-button, 3-light sequence puzzle (Diablo 2 statue-puzzle style) built on the existing `PuzzleController`/`PuzzleManager`/`PuzzleResultChannelSO` system.

**Architecture:** A pure C# state machine (`ButtonSequenceState`) tracks sequence progress and is fully unit tested. A thin `ButtonSequencePuzzleController : PuzzleController` MonoBehaviour wires 3 `UnityEngine.UI.Button`s and 3 `UnityEngine.UI.Image` light indicators to that state machine, following the exact pattern `ClockPuzzleController` uses (Escape-to-cancel, `Complete(PuzzleResult)`). The existing generic `ChestInteractable` opens the new prefab — no changes to shared puzzle-system files.

**Tech Stack:** Unity, C#, Unity UI (`UnityEngine.UI`), new Input System (`UnityEngine.InputSystem`), NUnit via Unity Test Framework (EditMode tests).

## Global Constraints

- Sequence order is fixed data set in the Inspector per puzzle instance — never randomized or player-defined (per spec).
- A wrong press must NOT raise any `PuzzleResult` — the puzzle stays open and resets in place (per spec). Only a fully correct sequence (`Success`) or Escape (`Cancelled`) end the puzzle.
- No new interactable script — reuse `ChestInteractable` as-is (its prefab field is typed `PuzzleController`).
- Lights are `UnityEngine.UI.Image` color swaps only — no `Light`, `Renderer`, or `Animator` (per spec, matches `ClockPuzzleController`).
- Reference spec: `docs/superpowers/specs/2026-07-04-button-sequence-puzzle-design.md`.

---

### Task 1: `ButtonSequenceState` — pure sequence-matching logic (TDD)

**Files:**
- Create: `Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequenceState.cs`
- Test: `Assets/Tests/EditMode/Puzzle/ButtonSequenceStateTests.cs`

**Interfaces:**
- Produces: `Puzzle.ButtonSequencePuzzle.ButtonSequenceState`, constructor `ButtonSequenceState(int[] correctSequence)`, method `bool Press(int buttonIndex)`, property `int Progress { get; }`, property `bool IsSolved { get; }`. Task 2 consumes all four.

This class has zero Unity dependencies (no `MonoBehaviour`, no UI types), so it is fully testable in EditMode without any GameObject setup — mirroring how `PuzzleResultChannelTests.cs` tests `PuzzleResultChannelSO` in isolation.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Puzzle/ButtonSequenceStateTests.cs`:

```csharp
using NUnit.Framework;
using Puzzle.ButtonSequencePuzzle;

namespace Tests.EditMode.Puzzle
{
    public class ButtonSequenceStateTests
    {
        [Test]
        public void Press_CorrectFirstIndex_ReturnsTrueAndAdvancesProgress()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            bool result = state.Press(1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, state.Progress);
            Assert.IsFalse(state.IsSolved);
        }

        [Test]
        public void Press_FullCorrectSequence_IsSolvedBecomesTrue()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            state.Press(2);
            bool lastResult = state.Press(0);

            Assert.IsTrue(lastResult);
            Assert.IsTrue(state.IsSolved);
        }

        [Test]
        public void Press_WrongIndex_ReturnsFalseAndResetsProgress()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            bool result = state.Press(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, state.Progress);
        }

        [Test]
        public void Press_WrongIndexAfterPartialProgress_ResetsToZero()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            bool result = state.Press(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, state.Progress);
        }

        [Test]
        public void Press_AfterReset_CanRetryFromStart()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            state.Press(0);
            bool retryResult = state.Press(1);

            Assert.IsTrue(retryResult);
            Assert.AreEqual(1, state.Progress);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run via Unity Editor: **Window → General → Test Runner → EditMode → Run All**.
Expected: compile error / all 5 tests fail — `ButtonSequenceState` does not exist yet.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequenceState.cs`:

```csharp
namespace Puzzle.ButtonSequencePuzzle
{
    public class ButtonSequenceState
    {
        private readonly int[] _correctSequence;

        public int Progress { get; private set; }
        public bool IsSolved => Progress == _correctSequence.Length;

        public ButtonSequenceState(int[] correctSequence)
        {
            _correctSequence = correctSequence;
        }

        public bool Press(int buttonIndex)
        {
            if (buttonIndex == _correctSequence[Progress])
            {
                Progress++;
                return true;
            }

            Progress = 0;
            return false;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run via Unity Editor: **Window → General → Test Runner → EditMode → Run All**.
Expected: all 5 tests in `ButtonSequenceStateTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequenceState.cs Assets/Tests/EditMode/Puzzle/ButtonSequenceStateTests.cs
git commit -m "feat: add button sequence state machine with tests"
```

---

### Task 2: `ButtonSequencePuzzleController` — UI wiring

**Files:**
- Create: `Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequencePuzzleController.cs`

**Interfaces:**
- Consumes: `Puzzle.ButtonSequencePuzzle.ButtonSequenceState` (Task 1) — `new ButtonSequenceState(int[])`, `.Press(int) : bool`, `.IsSolved : bool`. Also consumes `Puzzle.PuzzleController` base — `protected void Complete(PuzzleResult result)` (from `Assets/Scripts/Puzzle/PuzzleController.cs:8`) and `Puzzle.PuzzleResult` enum (`Success`, `Fail`, `Cancelled`).
- Produces: `Puzzle.ButtonSequencePuzzle.ButtonSequencePuzzleController`, a `PuzzleController` subclass, for Task 3 to attach to a prefab and wire in the Inspector. Public Inspector fields: `Button[] buttons`, `Image[] lightIndicators`, `int[] correctSequence`, `Color lightOffColor`, `Color lightOnColor`.

No automated test for this task: it requires live `UnityEngine.UI.Button`/`Image` references and a `PuzzleResultChannelSO` asset, which is exactly the same situation as `ClockPuzzleController` (`Assets/Scripts/Puzzle/ClockPuzzle/ClockPuzzleController.cs`) — that class also has no automated test, only manual Play Mode verification after scene wiring. Task 3 covers that manual verification.

- [ ] **Step 1: Write the controller**

Create `Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequencePuzzleController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Puzzle.ButtonSequencePuzzle
{
    public class ButtonSequencePuzzleController : PuzzleController
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private Image[] lightIndicators;
        [SerializeField] private int[] correctSequence = { 1, 2, 0 };
        [SerializeField] private Color lightOffColor = Color.gray;
        [SerializeField] private Color lightOnColor = Color.red;

        private ButtonSequenceState _state;

        private void Awake()
        {
            _state = new ButtonSequenceState(correctSequence);
            ResetLights();

            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => OnButtonPressed(index));
            }
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Complete(PuzzleResult.Cancelled);
        }

        private void OnButtonPressed(int index)
        {
            bool correct = _state.Press(index);
            if (correct)
            {
                lightIndicators[index].color = lightOnColor;
                if (_state.IsSolved)
                    Complete(PuzzleResult.Success);
            }
            else
            {
                ResetLights();
            }
        }

        private void ResetLights()
        {
            foreach (var indicator in lightIndicators)
                indicator.color = lightOffColor;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Open Unity Editor and let it recompile (or run `Assets → Open C# Project` and build). Expected: no compiler errors in the Console.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Puzzle/ButtonSequencePuzzle/ButtonSequencePuzzleController.cs
git commit -m "feat: add button sequence puzzle controller"
```

---

### Task 3: Build the prefab and wire the scene (manual, Unity Editor)

**Files:**
- Create: `Assets/Prefabs/Puzzle/Button Sequence Puzzle.prefab`
- Modify: `Assets/Scenes/SampleScene.unity`

**Interfaces:**
- Consumes: `Puzzle.ButtonSequencePuzzle.ButtonSequencePuzzleController` (Task 2) Inspector fields (`buttons`, `lightIndicators`, `correctSequence`, `lightOffColor`, `lightOnColor`); the existing `Puzzle.ChestInteractable` (`Assets/Scripts/Puzzle/ChestInteractable.cs`) Inspector fields (`puzzleManager`, `clockPuzzlePrefab` — accepts any `PuzzleController`, `resultChannel`); the existing `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel.asset`.

This task is GUI work in the Unity Editor and cannot be expressed as shell commands — follow these steps exactly, mirroring how "Clock Puzzle.prefab" was built (`Assets/Prefabs/Puzzle/Clock Puzzle.prefab`).

- [ ] **Step 1: Build the puzzle UI hierarchy**

In the Unity Editor, create a new empty scene or work in a scratch area of `SampleScene`:
1. Create a `Canvas` GameObject (Screen Space - Overlay), name it `Button Sequence Puzzle`.
2. Add a `Panel` (background) as a child.
3. Add 3 `Button` (TextMeshPro or legacy UI, matching whatever `Clock Puzzle.prefab` uses) children named `Button 0`, `Button 1`, `Button 2`, laid out side by side.
4. Add 3 `Image` children named `Light 0`, `Light 1`, `Light 2`, one above/near each button, each with `Color` set to gray (`#808080`) by default.

- [ ] **Step 2: Attach and wire the controller**

1. Add the `Button Sequence Puzzle Controller` component (from Task 2) to the root `Button Sequence Puzzle` GameObject.
2. In the Inspector, set:
   - `Buttons` → size 3 → drag `Button 0`, `Button 1`, `Button 2` in index order.
   - `Light Indicators` → size 3 → drag `Light 0`, `Light 1`, `Light 2` in the same index order.
   - `Correct Sequence` → size 3 → e.g. `1, 2, 0` (or your own designed order).
   - `Light Off Color` → gray, `Light On Color` → red (or match art direction).
   - `Result Channel` (inherited from `PuzzleController`) → drag `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel.asset`.

- [ ] **Step 3: Save as prefab**

Drag the `Button Sequence Puzzle` root GameObject into `Assets/Prefabs/Puzzle/` to create `Button Sequence Puzzle.prefab`. Delete the instance from the scene/scratch area afterward (the prefab asset is what matters).

- [ ] **Step 4: Add an interactable trigger to `SampleScene.unity`**

1. Open `Assets/Scenes/SampleScene.unity`.
2. Add a new GameObject (or reuse an existing interactable object in the scene, e.g. a lever/chest placeholder) with a `Chest Interactable` component (`Puzzle.ChestInteractable`).
3. Wire its Inspector fields:
   - `Puzzle Manager` → drag the scene's existing `PuzzleManager` object.
   - `Clock Puzzle Prefab` (field is generically typed `PuzzleController`) → drag the new `Button Sequence Puzzle.prefab`.
   - `Result Channel` → drag `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel.asset`.
4. Ensure whatever calls `OnInteract()` on this object (player interaction script) is hooked up the same way it is for the existing chest/clock interactable in the scene.

- [ ] **Step 5: Manual verification (Play Mode)**

Enter Play Mode and interact with the new object:
1. Confirm the puzzle UI opens and `Time.timeScale` pauses the rest of the scene.
2. Press buttons in the wrong order — confirm all lights turn off and progress resets (retry from start works).
3. Press buttons in the exact `correctSequence` order — confirm each press lights its indicator, and the puzzle closes (`Success`) after the last correct press.
4. Press Escape mid-sequence — confirm the puzzle closes (`Cancelled`) without needing to finish the sequence.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Prefabs/Puzzle/Button Sequence Puzzle.prefab" "Assets/Prefabs/Puzzle/Button Sequence Puzzle.prefab.meta" Assets/Scenes/SampleScene.unity
git commit -m "feat: wire up button sequence puzzle prefab and scene"
```
