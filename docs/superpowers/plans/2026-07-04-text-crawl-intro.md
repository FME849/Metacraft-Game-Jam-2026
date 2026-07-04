# Text Crawl Intro Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable, black-screen, upward-scrolling text crawl (flat "Star Wars style", no perspective) as a single prefab, and use it to build the game's opening `Intro.unity` scene, which transitions to `Scene01` on completion.

**Architecture:** A pure C# helper (`TextCrawlMath.ComputeScrollDistance`) computes the total scroll distance from viewport and content height, and is fully unit tested. A thin `TextCrawl` `MonoBehaviour` drives the actual scroll (advances a `RectTransform`'s `anchoredPosition.y` every frame, plays optional music, loads the next scene on completion) and is packaged into one reusable prefab, `TextCrawlScreen`, so every text-crawl scene (starting with `Intro.unity`) only overrides four Inspector fields (`lines`, `scrollSpeed`, `nextSceneName`, `backgroundMusic`) with no per-scene script or layout work.

**Tech Stack:** Unity, C#, TextMeshPro (`Unity.TextMeshPro`), `UnityEngine.UI` (`ContentSizeFitter`, `Image`), `UnityEngine.SceneManagement`, NUnit via Unity Test Framework (EditMode tests).

## Global Constraints

- Not skippable — no input of any kind advances or cancels the crawl (per spec).
- Flat vertical scroll only — no 3D perspective/tilt (per spec).
- Crawl text uses the **Cinzel** font: `Assets/Fonts/Cinzel-VariableFont_wght SDF.asset` (per user request).
- Content is a flat `string[]` set per-instance in the Inspector — no dependency on Yarn Spinner or the `.yarn`/`ClickDialoguePresenter` pipeline (per spec).
- A single fixed `scrollSpeed` (px/second) drives the whole block — no per-line durations (per spec).
- `nextSceneName` is a per-instance configurable string field, never hardcoded in code (per spec).
- The whole feature is one reusable prefab (`TextCrawlScreen`) — style/layout changes happen in one place, each scene instance only overrides four fields (per spec).
- Reference spec: `docs/superpowers/specs/2026-07-04-text-crawl-intro-design.md`.

---

### Task 1: `TextCrawlMath` — pure scroll-distance math (TDD)

**Files:**
- Create: `Assets/Scripts/TextCrawl/TextCrawl.asmdef`
- Modify: `Assets/Tests/EditMode/EditModeTests.asmdef`
- Create: `Assets/Scripts/TextCrawl/TextCrawlMath.cs`
- Test: `Assets/Tests/EditMode/TextCrawl/TextCrawlMathTests.cs`

**Interfaces:**
- Produces: `Metacraft.TextCrawl.TextCrawlMath.ComputeScrollDistance(float viewportHeight, float contentHeight) : float`. Task 2 consumes this.

This class has zero Unity dependencies (no `MonoBehaviour`, no UI types), so it is fully testable in EditMode without any GameObject setup — mirroring how `PuzzleResultChannelTests.cs` tests `PuzzleResultChannelSO` in isolation, and how `ButtonSequenceState` was pulled out as pure logic in the button-sequence-puzzle plan.

- [ ] **Step 1: Create the `TextCrawl` assembly definition**

Create `Assets/Scripts/TextCrawl/TextCrawl.asmdef`:

```json
{
    "name": "TextCrawl",
    "references": [
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

This mirrors `Assets/Scripts/Puzzle/Puzzle.asmdef` exactly, swapping its `Unity.InputSystem` reference for `Unity.TextMeshPro` (needed by Task 2's `TMP_Text` field). `UnityEngine.UI` types (`Image`, `ContentSizeFitter`) and `UnityEngine.SceneManagement` need no explicit reference — they are engine modules, auto-referenced the same way `Puzzle.asmdef` gets `UnityEngine.UI.Image` for free in `ClockPuzzleController`.

- [ ] **Step 2: Reference `TextCrawl` from the EditMode test assembly**

Open `Assets/Tests/EditMode/EditModeTests.asmdef` and add `"TextCrawl"` to the `references` array, alongside the existing `"Puzzle"` entry:

```json
{
    "name": "EditModeTests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Puzzle",
        "TextCrawl"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Open Unity Editor once to let it generate `.meta` files**

Open the project in the Unity Editor and let it finish importing/compiling. This generates `Assets/Scripts/TextCrawl/TextCrawl.asmdef.meta` and creates the `Assets/Scripts/TextCrawl/` folder's `.meta` — required before these files can be committed.
Expected: Console shows no compile errors (the asmdef currently has no scripts in it yet, which is valid).

- [ ] **Step 4: Write the failing tests**

Create `Assets/Tests/EditMode/TextCrawl/TextCrawlMathTests.cs`:

```csharp
using NUnit.Framework;
using Metacraft.TextCrawl;

namespace Tests.EditMode.TextCrawl
{
    public class TextCrawlMathTests
    {
        [Test]
        public void ComputeScrollDistance_ReturnsSumOfViewportAndContentHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(1080f, 600f);

            Assert.AreEqual(1680f, distance);
        }

        [Test]
        public void ComputeScrollDistance_ZeroContentHeight_ReturnsViewportHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(1080f, 0f);

            Assert.AreEqual(1080f, distance);
        }

        [Test]
        public void ComputeScrollDistance_ZeroViewportHeight_ReturnsContentHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(0f, 600f);

            Assert.AreEqual(600f, distance);
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run via Unity Editor: **Window → General → Test Runner → EditMode → Run All**.
Expected: compile error / all 3 tests fail — `TextCrawlMath` does not exist yet.

- [ ] **Step 6: Write minimal implementation**

Create `Assets/Scripts/TextCrawl/TextCrawlMath.cs`:

```csharp
namespace Metacraft.TextCrawl
{
    public static class TextCrawlMath
    {
        public static float ComputeScrollDistance(float viewportHeight, float contentHeight)
        {
            return viewportHeight + contentHeight;
        }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run via Unity Editor: **Window → General → Test Runner → EditMode → Run All**.
Expected: all 3 tests in `TextCrawlMathTests` PASS.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/TextCrawl/TextCrawl.asmdef Assets/Scripts/TextCrawl/TextCrawl.asmdef.meta \
        Assets/Tests/EditMode/EditModeTests.asmdef \
        Assets/Scripts/TextCrawl/TextCrawlMath.cs \
        Assets/Tests/EditMode/TextCrawl/TextCrawlMathTests.cs Assets/Tests/EditMode/TextCrawl/TextCrawlMathTests.cs.meta
git commit -m "feat: add text crawl scroll-distance math with tests"
```

---

### Task 2: `TextCrawl` — scrolling behavior component

**Files:**
- Create: `Assets/Scripts/TextCrawl/TextCrawl.cs`

**Interfaces:**
- Consumes: `Metacraft.TextCrawl.TextCrawlMath.ComputeScrollDistance(float, float) : float` (Task 1).
- Produces: `Metacraft.TextCrawl.TextCrawl`, a `MonoBehaviour` with serialized fields `TMP_Text crawlText`, `RectTransform crawlContent`, `RectTransform viewport`, `AudioSource audioSource`, `string[] lines`, `float scrollSpeed`, `string nextSceneName`, `AudioClip backgroundMusic`. Task 3 attaches this to the `TextCrawlScreen` prefab and wires these fields in the Inspector.

No automated test for this task: it requires live `RectTransform`/`TMP_Text`/`Canvas` references, exactly the same situation as `ClockPuzzleController` and `ButtonSequencePuzzleController` (no automated test, manual Play Mode verification only). Task 4 covers that manual verification.

- [ ] **Step 1: Write the component**

Create `Assets/Scripts/TextCrawl/TextCrawl.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Metacraft.TextCrawl
{
    public sealed class TextCrawl : MonoBehaviour
    {
        [SerializeField] private TMP_Text crawlText;
        [SerializeField] private RectTransform crawlContent;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private string[] lines;
        [SerializeField, Min(1f)] private float scrollSpeed = 50f;
        [SerializeField] private string nextSceneName;
        [SerializeField] private AudioClip backgroundMusic;

        private float scrollDistance;
        private float traveled;
        private bool finished;

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            crawlText.text = string.Join("\n\n", lines);
            LayoutRebuilder.ForceRebuildLayoutImmediate(crawlContent);

            float viewportHeight = viewport.rect.height;
            float contentHeight = crawlContent.rect.height;
            scrollDistance = TextCrawlMath.ComputeScrollDistance(viewportHeight, contentHeight);
            traveled = 0f;

            Vector2 startPosition = crawlContent.anchoredPosition;
            startPosition.y = -contentHeight;
            crawlContent.anchoredPosition = startPosition;

            if (backgroundMusic != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            float delta = scrollSpeed * Time.deltaTime;
            crawlContent.anchoredPosition += new Vector2(0f, delta);
            traveled += delta;

            if (traveled >= scrollDistance)
            {
                finished = true;

                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            }
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = crawlText != null && crawlContent != null && viewport != null && audioSource != null;
            if (!hasReferences)
            {
                Debug.LogWarning("TextCrawl needs Crawl Text, Crawl Content, Viewport, and Audio Source references assigned in the Inspector.", this);
            }

            return hasReferences;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Open Unity Editor and let it recompile. Expected: no compiler errors in the Console.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TextCrawl/TextCrawl.cs
git commit -m "feat: add text crawl scroll behavior component"
```

---

### Task 3: Build the `TextCrawlScreen` prefab (manual, Unity Editor)

**Files:**
- Create: `Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab`

**Interfaces:**
- Consumes: `Metacraft.TextCrawl.TextCrawl` (Task 2) Inspector fields (`crawlText`, `crawlContent`, `viewport`, `audioSource`, `lines`, `scrollSpeed`, `nextSceneName`, `backgroundMusic`); font asset `Assets/Fonts/Cinzel-VariableFont_wght SDF.asset`.

This task is GUI work in the Unity Editor and cannot be expressed as shell commands.

- [ ] **Step 1: Build the hierarchy in a scratch scene**

In the Unity Editor, in a new empty scene or a scratch area:
1. Create a `Canvas` GameObject, name it `TextCrawlScreen`.
   - Render Mode: `Screen Space - Overlay`.
   - Add a `Canvas Scaler` component: `UI Scale Mode` = `Scale With Screen Size`, `Reference Resolution` = `1920 x 1080`, `Screen Match Mode` = `Match Width Or Height` with `Match` = `0` (matches the `Canvas Scaler` settings already used in `Scene01.unity` and `SampleScene.unity`).
   - Add an `Audio Source` component to this root GameObject. Uncheck `Play On Awake`.
2. Add a child GameObject `Background`:
   - `Image` component, color `#000000` (opaque black).
   - `RectTransform` anchors stretched full (anchor min `0,0`, anchor max `1,1`), all offsets `0`.
3. Add a child GameObject `CrawlContent`:
   - `TextMeshProUGUI` component (`crawlText`).
     - Font Asset: `Cinzel-VariableFont_wght SDF` (`Assets/Fonts/Cinzel-VariableFont_wght SDF.asset`).
     - Color: white.
     - Alignment: Center / Top (horizontally centered, text grows downward from its own top).
   - `RectTransform` (`crawlContent`): anchor min `(0, 0)`, anchor max `(1, 0)` (stretched horizontally, anchored to the bottom), pivot `(0.5, 0)`.
   - Add a `Content Size Fitter` component: `Vertical Fit` = `Preferred Size` (so the `RectTransform`'s height tracks the text's actual rendered height).

- [ ] **Step 2: Attach and wire `TextCrawl`**

1. Add the `Text Crawl` component (from Task 2) to the root `TextCrawlScreen` GameObject.
2. In the Inspector, set:
   - `Crawl Text` → drag `CrawlContent` (its `TextMeshProUGUI`).
   - `Crawl Content` → drag `CrawlContent` (its `RectTransform`).
   - `Viewport` → drag the root `TextCrawlScreen` Canvas's own `RectTransform`.
   - `Audio Source` → drag the root's `Audio Source` component.
   - `Scroll Speed` → leave at the default (`50`).
   - `Lines`, `Next Scene Name`, `Background Music` → leave empty (per-instance override, set in Task 4).

- [ ] **Step 3: Save as prefab**

Drag the `TextCrawlScreen` root GameObject into `Assets/Prefabs/TextCrawl/` to create `TextCrawlScreen.prefab`. Delete the instance from the scratch scene afterward (the prefab asset is what matters).

- [ ] **Step 4: Commit**

```bash
git add "Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab" "Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab.meta"
git commit -m "feat: add reusable TextCrawlScreen prefab"
```

---

### Task 4: Build `Intro.unity`, wire content, update build settings, verify

**Files:**
- Create: `Assets/Scenes/Intro.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Consumes: `Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab` (Task 3) and its `Text Crawl` Inspector fields (`lines`, `scrollSpeed`, `nextSceneName`, `backgroundMusic`).

This task is GUI work in the Unity Editor and cannot be expressed as shell commands.

- [ ] **Step 1: Create the scene**

In the Unity Editor: **File → New Scene** (Basic template), save as `Assets/Scenes/Intro.unity`.

- [ ] **Step 2: Place and configure the prefab instance**

1. Delete the default `Main Camera` and any default lighting objects from the new scene — a `Screen Space - Overlay` canvas needs no camera.
2. Drag `Assets/Prefabs/TextCrawl/TextCrawlScreen.prefab` into the scene.
3. On this instance, override the `Text Crawl` component's fields:
   - `Lines` (size 5):
     - `In the smoke-choked reaches of the Empire, a man of standing does not simply die.`
     - `House Ashworth built its fortune on the Continuance Act — the right to return, once, through means the Church calls unholy and the Registry calls law.`
     - `Edmund Ashworth never asked for the policy his family bought in his name.`
     - `But when the eastern rail line failed him, the policy activated without his consent.`
     - `Now he wakes in a stranger's ward, and the doctors are asking questions no dying man should have to answer twice.`
   - `Scroll Speed` → `40`.
   - `Next Scene Name` → `Scene01`.
   - `Background Music` → leave empty (no music asset exists in the project yet; assign one later without touching code, per the spec's optional-audio requirement).

- [ ] **Step 3: Update Build Settings**

1. Open `Assets/Scenes/Intro.unity` and `Assets/Scenes/Scene01.unity` so both are open, or add them individually.
2. **File → Build Profiles / Build Settings**.
3. Click **Add Open Scenes**, or drag `Assets/Scenes/Intro.unity` and `Assets/Scenes/Scene01.unity` from the Project window into the **Scenes In Build** list.
4. Reorder the list so `Intro` is index `0` (drag it to the top). Existing entries (`SampleScene`) stay in the list below it — do not remove them.

- [ ] **Step 4: Manual verification (Play Mode)**

1. Open `Intro.unity` and enter Play Mode.
2. Confirm the screen is black with white Cinzel-font text starting off-screen at the bottom.
3. Confirm the text scrolls straight upward at a constant speed, with no tilt/perspective.
4. Confirm no key press, click, or Escape skips or interrupts the crawl.
5. Let the crawl finish — confirm the game automatically loads `Scene01` once the text has fully scrolled past the top of the screen.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Intro.unity Assets/Scenes/Intro.unity.meta ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: add Intro scene using TextCrawlScreen prefab"
```
