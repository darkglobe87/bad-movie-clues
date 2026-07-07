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
- `Assets/Editor/PackageInstaller.cs` is a throwaway batch-mode helper used
  once to resolve package versions; safe to delete once packages are
  confirmed present in `Packages/manifest.json` / `packages-lock.json`.
- No bootstrap scene or composition-root script exists yet — that's the next
  M0 step, followed by M1 (content model + authoring tool).
