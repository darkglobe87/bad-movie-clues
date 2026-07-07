# Bad Movie Clues (Unity Rebuild)

## What this game is
A mobile hangman-style word game. Each level shows a badly-drawn picture and a
terrible, overly-literal description of a movie; the player guesses the movie
title one letter at a time. Three clue types cost in-game **coins**: revealing
the **picture**, revealing a **character clue** (a non-title character's
name), and revealing a **letter hint**. Coins can also be purchased via the
Play Store (IAP) — stubbed until milestone M7.

This is a from-scratch Unity rebuild of an earlier Kotlin/Compose prototype.
No code from that prototype is reused. Only two things carry over as content:
the tone/text of the "bad descriptions" (see `Assets/_Project/Content/`) and
the owner's hand-made "bad art" images.

## Golden rules
1. **Gameplay and economy logic lives in plain C# POCOs — never in a
   MonoBehaviour.** `PuzzleState`, `ICurrencyService`, `HintService`, etc.
   must be constructible and testable with zero Unity API calls. MonoBehaviours
   are thin glue: they read services and update views, nothing else.
2. **Every external dependency sits behind an interface.** Ads, IAP,
   analytics, remote config, cloud save, and content delivery are all
   `I...Service` interfaces. Only no-op/local stub implementations exist
   until milestone M7 — call sites never change when the stubs are swapped
   for real SDKs.
3. **One system per milestone.** Do not batch-generate unrelated scripts in
   one pass. Finish and verify a milestone (tests green / Play mode works)
   before starting the next.
4. **Respect assembly boundaries** (see below). If a compile forces a
   dependency across a boundary that shouldn't exist, that's a signal the
   design needs to change, not the asmdef reference.
5. **Images are authored offline and shipped as files.** There is no image
   generation code anywhere in the app. New art is produced by the owner
   outside Unity and imported through the M1 authoring tool.
6. Write EditMode tests for all logic in `Puzzle` and `Economy` as it's
   built, not after.

## Tech stack
- Unity 6 LTS (6000.3.x)
- uGUI + TextMeshPro for UI (TMP ships inside Unity 6's UI package — first
  time you drop a TMP component in a scene, accept the "Import TMP Essential
  Resources" prompt; no separate package entry needed)
- Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) for catalog parsing —
  `JsonUtility` cannot parse a top-level JSON array, which the catalog is
- Addressables for images + (later) the remote level catalog
- PrimeTween for UI animation (button squish, letter-tile pop) — free, MIT,
  zero-alloc; added via npm scoped registry in `Packages/manifest.json`
- Unity Test Framework, EditMode tests only for now
- Unity 6 `Awaitable` for async loads (no UniTask dependency)

## Assembly layout
```
Assets/_Project/
  Scripts/
    Core/      BadMovieClues.Core       — GameController, bootstrap/composition root
    Data/      BadMovieClues.Data       — LevelData, LevelCatalog, IContentProvider + impls
    Puzzle/    BadMovieClues.Puzzle     — PuzzleState (pure C#, no UnityEngine dependency beyond primitives)
    Economy/   BadMovieClues.Economy    — ICurrencyService, HintService, GameConfig SO
    UI/        BadMovieClues.UI         — views, PrimeTween animation glue
    Services/  BadMovieClues.Services   — I*Service interfaces + stub implementations
    Editor/    BadMovieClues.Editor     — movie authoring/curation window (Editor platform only)
  Tests/EditMode/ BadMovieClues.Tests   — tests for Puzzle + Economy
  Content/   — bundled starter catalog JSON + curated images
  Art/ Prefabs/ Scenes/
```
Dependency direction: `UI → Core → {Data, Puzzle, Economy, Services}`.
`Data`, `Puzzle`, `Economy`, `Services` do not depend on `UI` or each other
except where explicitly needed (e.g. `Core` wires `Economy`'s `HintService`
to `Data`'s image loading). `Editor` may reference `Data`/`Core` but nothing
may reference `Editor` (it's excluded from player builds).

## Milestones
See the plan file this project was scaffolded from for the full milestone
list (M0 setup → M7 Android build + real SDKs). Work through them in order;
each has its own verification step.

## Content notes
- `Assets/_Project/Content/StarterCatalog.json` is seeded from the owner's
  own descriptions/character names written for the earlier prototype — reused
  as content, not code. 36 movies total; 7 already have a matching `imageKey`
  (`img_jaws`, `img_et`, `img_shrek`, `img_toy_story`,
  `img_lord_of_the_rings`, `img_matrix`, `img_pulp_fiction`).
- `Assets/_Project/Content/Images/` holds the owner's curated "bad art"
  images, imported as-is. Two images could not be confidently matched to a
  movie and are named `_unmatched_bird_tower.jpg` (a bird/bat silhouette over
  a purple tower block) and `_unmatched_eye_towers.jpg` (a red eye above two
  black spires) — resolve these through the M1 authoring tool rather than
  guessing.
- `img_pulp_fiction.png` is a large (~10MB) source PNG. Fine for the editor
  vertical slice, but before importing many more images at that size,
  downscale/compress sources so the repo doesn't bloat — Unity's texture
  importer compresses for the build regardless, but the source asset size
  still hits git and Library.

## Setup status (M0)
- Unity project scaffolded via `Unity.exe -createProject`, folder/asmdef
  layout created, starter catalog + curated images imported.
- Packages added: `com.unity.nuget.newtonsoft-json`, `com.unity.addressables`,
  `com.unity.test-framework` (versions resolved by Package Manager, not
  hand-pinned), plus PrimeTween via an npm scoped registry pointed at
  `com.kyrylokuzyk` in `Packages/manifest.json`. TextMeshPro needs no separate
  package — accept the "Import TMP Essential Resources" prompt the first time
  a TMP component is used in a scene.
- `Assets/Editor/PackageInstaller.cs` was a throwaway batch-mode helper used
  once to resolve package versions; already deleted.

## Setup status (M1)
- Data layer implemented: `LevelData`, `LevelCatalog`, `IContentProvider`,
  `BundledContentProvider` (Newtonsoft JSON from `Resources/StarterCatalog`,
  images via Addressables keyed by `ImageKey`, returned as `Sprite`).
- `Assets/_Project/Content/StarterCatalog.json` moved to
  `Assets/_Project/Content/Resources/StarterCatalog.json` so it loads via
  `Resources.Load<TextAsset>` — this is the *bundled* catalog path; the
  remote one (M6) will use a different provider behind the same interface.
- `MovieAuthoringWindow` (`Bad Movie Clues > Movie Catalog Editor` menu) lets
  you add/edit/delete movies and assign images; assigning an image
  auto-registers it as Addressable via `AddressableImageUtility` (sets
  Texture Type = Sprite, address = imageKey, group `BadMovieClues-Images`).
  A "Sync All Images To Addressables" button re-runs that for every level.
- The 7 already-matched curated images (jaws/et/shrek/toy_story/
  lord_of_the_rings/matrix/pulp_fiction) are synced and Addressable now.
  The 2 `_unmatched_*` images still need a human to assign them to a movie
  via the authoring tool.
- EditMode tests in `LevelCatalogTests.cs` cover top-level-array parsing
  (the reason Newtonsoft is used over `JsonUtility`), optional-field
  defaults, `LevelCatalog.FindById`, and a regression check that the bundled
  catalog still has all 36 unique movies with the expected image keys. All
  passing (`Unity.exe -runTests -testPlatform EditMode`).
- Gotcha worth remembering: `BadMovieClues.Tests.asmdef` has
  `overrideReferences: true`, so precompiled plugin DLLs (Newtonsoft.Json,
  nunit) must be listed by filename in `precompiledReferences`, not just by
  name in `references` — the latter alone silently fails to resolve.
- Addressables runtime loading (`Addressables.LoadAssetAsync<Sprite>`) is
  *not* exercised by EditMode tests — Addressables needs a running/Play Mode
  context to initialize reliably. First real verification of that path is
  M3, once there's an actual Play Mode scene to test in.
- No bootstrap scene or composition-root script exists yet — that's M3, after
  the pure Puzzle logic (M2).

## Setup status (M2)
- `PuzzleState` implemented in `BadMovieClues.Puzzle` — pure C#, no
  MonoBehaviour, no UnityEngine reference at all (the asmdef has
  `noEngineReferences: true`, so this is compiler-enforced, not just
  convention). Only letters are guessable; spaces/punctuation/digits are
  revealed from the start. Case-insensitive guessing, `Won`/`Lost` events
  fire exactly once, guesses after game-over or repeat guesses are rejected
  without changing state, and a title with zero letters (e.g. `"1984"`) is
  trivially won at construction.
- 14 EditMode tests in `PuzzleStateTests.cs` cover masking, repeated-letter
  reveals, case-insensitivity, punctuation/digit handling, a sequel title
  with a digit, win/loss event firing exactly once, post-game-over guesses,
  and constructor argument validation. All 19 project tests passing.
- Next is M3: wire `GameController` + a bootstrap scene around
  `BundledContentProvider` + `PuzzleState` for a minimal playable loop in
  the editor (no economy, no juice yet).

## Setup status (M3)
- `GameController` (Core, plain C#) loads the catalog, picks a level, loads
  its image, constructs a `PuzzleState`, and re-broadcasts
  `LevelLoaded`/`Won`/`Lost` events.
- **Architecture deviation from the original asmdef table, intentional:**
  the composition root (`GameBootstrap`) lives in `BadMovieClues.UI`, not
  Core. Core must never depend on UI (see dependency direction above), and
  a bootstrap that wires a concrete view needs to know about it - so it
  belongs at the top of the dependency graph (UI), not in Core.
- `GameHud` (UI) is a deliberately plain MonoBehaviour view: legacy
  `UnityEngine.UI` (`Text`/`Image`/`Button`/`GridLayoutGroup`), not
  TextMeshPro - upgrading to TMP is explicitly M5's job, not M3's.
- `Assets/_Project/Scenes/Bootstrap.unity` is the real playable scene:
  Canvas, description/blanks text, picture image, a runtime-built A-Z
  keyboard, `GameHud` + `GameBootstrap`. Registered in Build Settings.
- **Package gotcha found and fixed:** `com.unity.modules.ui` (a built-in
  module, already present) is *not* the same as `com.unity.ugui` (the actual
  package providing legacy `Text`/`Image`/`Button`) - the latter was never
  installed until M3. `BadMovieClues.UI.asmdef`'s reference was also wrong
  (`"Unity.ugui"` instead of `"UnityEngine.UI"`, the real assembly name) -
  both fixed. Installing a package requires the whole project to compile
  first, so when GameHud.cs itself was the thing failing to compile, the
  broken files had to be moved out temporarily, package installed, then
  restored.
- **Real Addressables bug found and fixed:** `AddressableImageUtility` (M1)
  set the texture import type to Sprite, but Addressables had already
  cached the entry's type as `Texture2D` at registration time, so
  `Addressables.LoadAssetAsync<Sprite>(imageKey)` threw `InvalidKeyException`
  at runtime (only caught because M3's verification actually exercised the
  real Addressables load path, which M1's EditMode tests deliberately did
  not). Fixed by loading `Texture2D` (matching what's actually registered)
  and wrapping it in a `Sprite` via `Sprite.Create` inside
  `BundledContentProvider`; `AddressableImageUtility` no longer touches
  texture import type at all.
- **Verification approach:** rather than toggling real Play Mode from a
  batch script (which triggers a domain reload that wipes static state
  needed to poll for completion - a real trap), M3 was verified by calling
  `GameController` directly from a throwaway Editor script: load the
  catalog, load the "jaws" level specifically (it has a real image, unlike
  index 0), confirm `CurrentImage` resolved via real Addressables, guess
  every letter, confirm `IsWon`. This exercises the actual runtime path
  end-to-end. It does **not** verify the MonoBehaviour/UI wiring
  (`GameHud`/`GameBootstrap`/button clicks) - that needs a human to open
  `Bootstrap.unity` and press Play, since there's no way to drive the Unity
  Editor GUI from this environment (unlike web dev, where a browser preview
  tool exists). **Please open the project and hit Play at least once to
  confirm the on-screen keyboard/description/picture actually look right.**
- Next is M4: `ICurrencyService` + `HintService` + `GameConfig` (coins,
  hint costs) and gating the hint buttons.
