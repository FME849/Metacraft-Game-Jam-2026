# Puzzle System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable puzzle open/close/result flow using a ScriptableObject event channel, decoupled from main gameplay.

**Architecture:** `PuzzleManager` (singleton MonoBehaviour) instantiates puzzle prefabs and owns lifecycle (pause/resume). Puzzle prefabs inherit from `PuzzleController` (abstract MonoBehaviour) and call `Complete(result)` to fire a `PuzzleResultChannelSO` event. Interactables subscribe to the same SO and react to results independently.

**Tech Stack:** Unity 6 (URP 2D), C#, Unity Test Framework 1.5.1 (EditMode tests), New Input System 1.14.0

## Global Constraints

- All scripts go under `Assets/Scripts/Puzzle/` (or subdirectories)
- Assembly definitions are NOT used — all code lands in Assembly-CSharp
- EditMode tests go under `Assets/Tests/EditMode/Puzzle/` with a dedicated `.asmdef`
- Puzzle prefabs must use `unscaledDeltaTime` for any animations (game pauses via `Time.timeScale = 0`)
- Every puzzle prefab requires a `PuzzleResultChannelSO` reference in Inspector
- `PuzzleManager` is placed once in the main scene — not DontDestroyOnLoad

---

### Task 1: PuzzleResult enum

**Files:**
- Create: `Assets/Scripts/Puzzle/PuzzleResult.cs`

**Interfaces:**
- Produces: `enum PuzzleResult { Success, Fail, Cancelled }` — used by all subsequent tasks

- [ ] **Step 1: Create folder and file**

Create `Assets/Scripts/Puzzle/PuzzleResult.cs`:

```csharp
namespace Puzzle
{
    public enum PuzzleResult { Success, Fail, Cancelled }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity. Check the Console window — no errors. The enum appears in IntelliSense when typing `PuzzleResult.` in any script.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Puzzle/PuzzleResult.cs
git commit -m "add PuzzleResult enum"
```

---

### Task 2: PuzzleResultChannelSO + EditMode test

**Files:**
- Create: `Assets/Scripts/Puzzle/PuzzleResultChannelSO.cs`
- Create: `Assets/Tests/EditMode/Puzzle/PuzzleResultChannelTests.cs`
- Create: `Assets/Tests/EditMode/EditModeTests.asmdef`
- Create (in Unity Editor): `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel.asset`

**Interfaces:**
- Consumes: `PuzzleResult` from Task 1
- Produces:
  - `PuzzleResultChannelSO.OnRaised` — `event Action<PuzzleResult>`
  - `PuzzleResultChannelSO.Raise(PuzzleResult)` — fires `OnRaised`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/EditModeTests.asmdef`:

```json
{
    "name": "EditModeTests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "noEngineReferences": false
}
```

Create `Assets/Tests/EditMode/Puzzle/PuzzleResultChannelTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Puzzle;

namespace Tests.EditMode.Puzzle
{
    public class PuzzleResultChannelTests
    {
        [Test]
        public void Raise_FiresOnRaisedWithCorrectResult()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            PuzzleResult received = default;
            channel.OnRaised += r => received = r;

            channel.Raise(PuzzleResult.Success);

            Assert.AreEqual(PuzzleResult.Success, received);
        }

        [Test]
        public void Raise_NoSubscribers_DoesNotThrow()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            Assert.DoesNotThrow(() => channel.Raise(PuzzleResult.Cancelled));
        }

        [Test]
        public void OnRaised_AfterUnsubscribe_DoesNotFire()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            int callCount = 0;
            System.Action<PuzzleResult> handler = _ => callCount++;

            channel.OnRaised += handler;
            channel.OnRaised -= handler;
            channel.Raise(PuzzleResult.Success);

            Assert.AreEqual(0, callCount);
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

In Unity: Window → General → Test Runner → EditMode tab → Run All.
Expected: 3 failures — `PuzzleResultChannelSO` does not exist yet.

- [ ] **Step 3: Implement PuzzleResultChannelSO**

Create `Assets/Scripts/Puzzle/PuzzleResultChannelSO.cs`:

```csharp
using System;
using UnityEngine;

namespace Puzzle
{
    [CreateAssetMenu(menuName = "Puzzle/Result Channel")]
    public class PuzzleResultChannelSO : ScriptableObject
    {
        public event Action<PuzzleResult> OnRaised;

        public void Raise(PuzzleResult result) => OnRaised?.Invoke(result);
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Window → General → Test Runner → EditMode → Run All.
Expected: 3 tests pass, 0 failures.

- [ ] **Step 5: Create the SO asset in Unity Editor**

In the Project window: right-click `Assets/ScriptableObjects/Puzzle/` (create folder if needed) → Create → Puzzle → Result Channel. Name it `PuzzleResultChannel`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Puzzle/PuzzleResultChannelSO.cs \
        Assets/Tests/EditMode/EditModeTests.asmdef \
        Assets/Tests/EditMode/Puzzle/PuzzleResultChannelTests.cs \
        Assets/ScriptableObjects/Puzzle/
git commit -m "add PuzzleResultChannelSO with EditMode tests"
```

---

### Task 3: PuzzleController abstract base

**Files:**
- Create: `Assets/Scripts/Puzzle/PuzzleController.cs`

**Interfaces:**
- Consumes: `PuzzleResultChannelSO.Raise(PuzzleResult)` from Task 2
- Produces:
  - `PuzzleController.Complete(PuzzleResult)` — protected, called by subclasses
  - `PuzzleController.OnPuzzleOpened()` — virtual, called by PuzzleManager after instantiation

- [ ] **Step 1: Create PuzzleController**

Create `Assets/Scripts/Puzzle/PuzzleController.cs`:

```csharp
using UnityEngine;

namespace Puzzle
{
    public abstract class PuzzleController : MonoBehaviour
    {
        [SerializeField] protected PuzzleResultChannelSO resultChannel;

        protected void Complete(PuzzleResult result) => resultChannel.Raise(result);

        public virtual void OnPuzzleOpened() { }
    }
}
```

- [ ] **Step 2: Verify compilation**

Unity Console — no errors. Confirm `PuzzleController` appears as a component option (though you cannot add abstract MonoBehaviours directly — this is expected).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Puzzle/PuzzleController.cs
git commit -m "add PuzzleController abstract base"
```

---

### Task 4: PuzzleManager

**Files:**
- Create: `Assets/Scripts/Puzzle/PuzzleManager.cs`

**Interfaces:**
- Consumes:
  - `PuzzleResultChannelSO.OnRaised` (subscribe/unsubscribe)
  - `PuzzleController.OnPuzzleOpened()`
- Produces:
  - `PuzzleManager.Open(PuzzleController prefab)` — called by interactables

- [ ] **Step 1: Implement PuzzleManager**

Create `Assets/Scripts/Puzzle/PuzzleManager.cs`:

```csharp
using UnityEngine;

namespace Puzzle
{
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
}
```

- [ ] **Step 2: Add PuzzleManager to main scene**

In Unity:
1. In the Hierarchy, create an empty GameObject named `PuzzleManager`
2. Add the `PuzzleManager` component to it
3. Drag `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel` into the `Result Channel` field in Inspector
4. Save the scene (Ctrl+S / Cmd+S)

- [ ] **Step 3: Verify compilation**

Unity Console — no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Puzzle/PuzzleManager.cs Assets/Scenes/
git commit -m "add PuzzleManager and wire into scene"
```

---

### Task 5: GearPuzzleController stub + prefab

**Files:**
- Create: `Assets/Scripts/Puzzle/GearPuzzle/GearPuzzleController.cs`
- Create (in Unity Editor): `Assets/Prefabs/Puzzle/GearPuzzle.prefab`

**Interfaces:**
- Consumes: `PuzzleController.Complete(PuzzleResult)` from Task 3
- Produces: `GearPuzzleController` — concrete MonoBehaviour, attach to GearPuzzle prefab

- [ ] **Step 1: Implement GearPuzzleController stub**

Create `Assets/Scripts/Puzzle/GearPuzzle/GearPuzzleController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Puzzle.GearPuzzle
{
    public class GearPuzzleController : PuzzleController
    {
        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Complete(PuzzleResult.Cancelled);
        }

        // Call this when gear puzzle logic determines the puzzle is solved
        public void OnPuzzleSolved() => Complete(PuzzleResult.Success);

        // Call this when puzzle reaches a fail condition (e.g. time up)
        public void OnPuzzleFailed() => Complete(PuzzleResult.Fail);
    }
}
```

- [ ] **Step 2: Create the GearPuzzle prefab**

In Unity:
1. Create an empty GameObject in the Hierarchy named `GearPuzzle`
2. Add the `GearPuzzleController` component to it
3. Drag `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel` into the `Result Channel` field in Inspector
4. Drag the `GearPuzzle` GameObject into `Assets/Prefabs/Puzzle/` to create the prefab
5. Delete the GameObject from the Hierarchy

- [ ] **Step 3: Verify compilation**

Unity Console — no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Puzzle/GearPuzzle/GearPuzzleController.cs \
        Assets/Prefabs/Puzzle/
git commit -m "add GearPuzzleController stub and prefab"
```

---

### Task 6: ChestInteractable + end-to-end integration

**Files:**
- Create: `Assets/Scripts/Puzzle/ChestInteractable.cs`

**Interfaces:**
- Consumes:
  - `PuzzleManager.Open(PuzzleController prefab)`
  - `PuzzleResultChannelSO.OnRaised` (subscribe/unsubscribe)
- Produces: `ChestInteractable.OnInteract()` — call this from player interaction code

- [ ] **Step 1: Implement ChestInteractable**

Create `Assets/Scripts/Puzzle/ChestInteractable.cs`:

```csharp
using UnityEngine;

namespace Puzzle
{
    public class ChestInteractable : MonoBehaviour
    {
        [SerializeField] PuzzleManager puzzleManager;
        [SerializeField] PuzzleController gearPuzzlePrefab;
        [SerializeField] PuzzleResultChannelSO resultChannel;

        public void OnInteract()
        {
            resultChannel.OnRaised += HandleResult;
            puzzleManager.Open(gearPuzzlePrefab);
        }

        private void HandleResult(PuzzleResult result)
        {
            resultChannel.OnRaised -= HandleResult;
            if (result == PuzzleResult.Success)
                gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: Wire ChestInteractable in scene**

In Unity:
1. Create an empty GameObject in Hierarchy named `Chest`
2. Add `ChestInteractable` component
3. Drag `PuzzleManager` GameObject into the `Puzzle Manager` field
4. Drag `Assets/Prefabs/Puzzle/GearPuzzle` prefab into the `Gear Puzzle Prefab` field
5. Drag `Assets/ScriptableObjects/Puzzle/PuzzleResultChannel` into the `Result Channel` field
6. Save the scene

- [ ] **Step 3: Integration test in Play Mode**

Press Play in Unity. In the Console, temporarily add a test call to verify the flow works:

Add this to `ChestInteractable.Start()` temporarily:
```csharp
private void Start() => OnInteract();
```

Expected:
- `Time.timeScale` becomes 0 (game pauses — you can verify in the Profiler or by adding `Debug.Log(Time.timeScale)` in `PuzzleManager.Open`)
- GearPuzzle prefab is instantiated in the Hierarchy
- Press ESC → prefab is destroyed, `Time.timeScale` returns to 1, `Chest` remains active
- To test Success: call `FindObjectOfType<GearPuzzleController>().OnPuzzleSolved()` from the Console tab

Remove the `Start()` test call after verifying.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Puzzle/ChestInteractable.cs Assets/Scenes/
git commit -m "add ChestInteractable and wire integration in scene"
```
