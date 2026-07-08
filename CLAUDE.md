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
16 total, M0–M15, worked in order with a verification step after each (see
the per-milestone "Setup status" sections below for what actually happened,
including any bugs found along the way). Full prompt text for each lives in
the plan file at
`C:\Users\matth\.claude\plans\i-ve-created-a-game-groovy-gadget.md` (local
only, not in this repo) — this list is just the index so it's visible from
GitHub/mobile too.

- M0 — Project setup & conventions
- M1 — Content model + authoring tool + bundled provider
- M2 — Pure puzzle logic + tests
- M3 — Core game loop (playable in editor)
- M4 — Economy + hint system
- M5 — Polished UI & juice *(completes the original "editor vertical slice")*
- M6 — Layout bugfix + theme foundation
- M7 — Skin the gameplay HUD + click SFX
- M8 — Persistent AppRoot + navigation skeleton
- M9 — Dust particle ambient background
- M10 — Main menu + splash + settings *(current)*
- M11 — Level select + progression
- M12 — Store shell (stubbed)
- M13 — Level-complete celebration (2D)
- M14 — Remote content pipeline *(was M6 before the M6–M13 art pass was inserted ahead of it)*
- M15 — Android build + monetization/liveops SDKs *(was M7)*

M6–M13 is a Visual/Art/UX pass added after M5 shipped a playable-but-plain
"editor vertical slice"; it bumped the original M6 (remote content) and M7
(Android + real SDKs) to M14/M15. See "Visual / Art / UX Pass" below for why.

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

## Setup status (M4)
- `ISaveService`/`LocalJsonSaveService` (Services): one JSON file per key
  under `Application.persistentDataPath` (directory injectable for tests).
- `ICurrencyService`/`CurrencyService` (Economy): persisted coin balance,
  `Add`/`TrySpend` (both reject negative amounts), `OnBalanceChanged` fires
  only on an actual change.
- `GameConfig` (Economy, ScriptableObject, asset at
  `Assets/_Project/Content/GameConfig.asset`): `StartingBalance` (100),
  `PictureHintCost` (50), `CharacterHintCost` (30), `LetterHintCost` (15).
  Tune these directly on the asset - no code change needed.
- `HintService` (Economy): picture/character hints are pure coin-gating (UI
  decides what "revealed" means); the letter hint picks a random *hidden*
  letter and guesses it on the puzzle's behalf - guaranteed correct, so it
  never costs a wrong guess. Correctly charges nothing if there's nothing
  left to hint (e.g. already fully guessed).
- `GameController` (Core) now also holds `Currency`/`Config` and exposes
  `TryRevealPictureHint`/`TryRevealCharacterHint`/`TryRevealLetterHint`,
  delegating to `HintService`. Core is allowed to depend on Economy per the
  stated dependency direction, so this needed no architecture exception.
- `GameHud` (UI): picture and character clue now start **hidden** each level
  (previously the picture was always shown - that was an M3 simplification,
  now corrected) and only reveal once their hint is purchased; a coin
  balance display; three hint buttons labelled with their live cost, each
  `Button.interactable` gated on affordability/already-revealed/game-over,
  refreshed on `Currency.OnBalanceChanged`.
- **Bug found twice, same root cause:** `PuzzleState.GuessedLetters` is
  typed `IReadOnlyCollection<char>`, which has no `Contains` method of its
  own - it needs `System.Linq`'s extension method. Missing that `using` hit
  both `HintService.cs` and, separately, `HintServiceTests.cs`. If you ever
  see "`IReadOnlyCollection<char>` does not contain a definition for
  `Contains`" again, this is why.
- 37 EditMode tests passing project-wide (18 new: currency spend/earn/
  persistence edge cases, real save/load round-trips via a temp directory,
  hint gating and the "exactly one hidden letter left" deterministic case).
- Runtime smoke test (same throwaway-script-calls-GameController-directly
  approach as M3) confirmed the real end-to-end economy path: starting
  balance loads correctly, all three hints spend the exact configured cost
  in sequence (100 → 50 → 20 → 5), the letter hint reveals exactly one new
  letter, and the puzzle is still winnable afterwards. Does not verify the
  new UI wiring (hint buttons/coin text on screen) - same caveat as M3,
  worth a manual Play-mode check.
- Next is M5: TextMeshPro + PrimeTween polish pass (button squish, letter
  tile pop, hint reveal transitions) - purely visual, no logic changes.

## Setup status (M5)
- **TMP Essential Resources imported** (`Assets/TextMesh Pro/`, ~4MB,
  commit as normal project content, not a build artifact). Needed before
  any `TextMeshProUGUI` renders correctly - triggered programmatically the
  same way Unity's own "Import TMP Essential Resources" menu item does
  internally: resolve the TextMeshPro package's `resolvedPath` via
  `PackageInfo.FindForAssembly`, then
  `AssetDatabase.ImportPackage(".../TMP Essential Resources.unitypackage", false)`.
- All `Text`/legacy UI text replaced with `TextMeshProUGUI` throughout
  `GameHud` (description, coin balance, character clue, keyboard keys, hint
  button labels).
- **Blanks row is now per-letter tiles, not one string.** `BuildBlanksRow`
  creates one `TextMeshProUGUI` per character position (in `blanksRoot`,
  rebuilt fresh each level since titles differ in length); `RefreshBlanks`
  diffs the previous vs. new `MaskedDisplay()` string character-by-character
  to detect which position just got revealed, and only pops *that* tile -
  this stays entirely UI-side string diffing, no new `PuzzleState` API.
- Win/loss status now appends to `descriptionText` (`"{description}\n\n{status}"`)
  rather than a dedicated status element - simpler, no new UI element needed,
  and the bad description isn't very useful once the round is already over.
- PrimeTween: `Tween.Scale(transform, endValue: 0.88f, duration: 0.08f,
  cycles: 2, cycleMode: CycleMode.Yoyo)` gives the button "squish" on every
  letter key and hint button click in one call (no manual chaining needed).
  `Tween.Scale(transform, endValue: 1f, duration: 0.35f, ease: Ease.OutElastic)`
  from a zeroed `localScale` gives the "pop" on newly-revealed letter tiles,
  the picture (once hinted), and the character clue text (once hinted).
- **Package reference bug found and fixed** (same class of mistake as
  M3's `Unity.ugui`): `BadMovieClues.UI.asmdef` referenced `"PrimeTween"`,
  but the package's actual runtime assembly is named `"PrimeTween.Runtime"`
  (confirmed by reading the installed package's own `.asmdef` file in
  `Library/PackageCache` directly, rather than guessing again).
- **No logic changed anywhere** - `GameController`, `PuzzleState`,
  `HintService`, `CurrencyService` are untouched. All 37 existing EditMode
  tests still pass unchanged; no new tests were needed since this milestone
  is explicitly visual-only per the plan.
- **Not verified:** the animations themselves, and whether the new
  TMP/tile layout actually looks right on screen. Compile-clean and
  logic-test-green is as far as this environment can confirm without a
  human pressing Play. Given M4's UI also hasn't been eyeballed yet, this
  is a good point to do **one combined Play-mode check** covering both
  milestones' UI at once rather than two separate check-ins.
- This completes the planned "editor vertical slice." Next is M6: a
  `RemoteContentProvider` (fetch the catalog + images from a URL, bundled
  catalog stays as offline fallback).

## Ad-hoc: sideload test APK
Before M6/M7, produced a real Android build to sanity-check the whole
pipeline on a physical device (not part of the milestone sequence, just a
"does this actually run on a phone" check requested early).
- `Assets/Editor/BuildAndroidTest.cs` — kept permanently (not a throwaway
  script like the others), menu item **Bad Movie Clues → Build Android
  Test APK**. Outputs to `Builds/Android/BadMovieClues-test.apk`
  (gitignored, like all build output).
- Uses **IL2CPP scripting backend + ARM64**, a **placeholder application
  identifier** (`com.badmovieclues.game`), and a **Development build**
  (debuggable, larger, not for release). The identifier and Development
  flag still need to change before any real Play Store submission - that's
  M15's job (renumbered from M7 once the Visual/Art/UX pass M6-M13 was
  inserted ahead of it - see the plan file).
- **Two real bugs found and fixed in sequence, both only surfacing at the
  Gradle stage (not compile-time):**
  1. First attempt used Mono2x + ARM64 - Mono doesn't support ARM64 on
     Android at all, so the build silently ended up with zero valid
     architectures selected ("Target architecture not specified").
     Switched to Mono2x + ARMv7, which "fixed" the build but was wrong:
  2. **ARMv7-only APKs don't install at all on recent high-end phones**
     (confirmed on a real device: Snapdragon 8 Elite). Qualcomm dropped
     32-bit app support entirely on recent flagship chips to save die
     space - this isn't a Unity/project issue, it's a real platform gap.
     The only correct fix is **IL2CPP + ARM64** (Mono cannot target ARM64
     on Android under any configuration - this is a hard platform
     limitation, not a setting to tune around). IL2CPP AOT-compiles all
     C# to native code via the NDK toolchain, so builds are substantially
     heavier/slower than Mono - expect this on future rebuilds too.
  Lesson: **don't assume ARMv7 "runs on any phone" for anything modern** -
  always build ARM64 via IL2CPP for real devices; only fall back to
  Mono/ARMv7 for a known-older test device if build speed is critical.
- Install: connect the phone via USB with USB debugging enabled, then
  `"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r Builds/Android/BadMovieClues-test.apk`
  (bundled adb, no separate Android Studio/SDK install needed).

## Visual / Art / UX Pass — see the plan file for full design
A separate ~9-milestone pass (M6-M13) was scoped after M5 to add theming,
a dust-particle background, a full main menu, splash screen, light-touch
audio, level-select progression, a stubbed store, and a 2D level-complete
celebration. The original M6 (remote content) and M7 (Android + real SDKs)
are renumbered M14/M15 to follow it. Full design detail, palette, asset
list, and milestone prompts live in the plan file at
`C:\Users\matth\.claude\plans\i-ve-created-a-game-groovy-gadget.md` under
"Visual / Art / UX Pass (M6-M13)" - not duplicated here to avoid drift
between two copies of the same plan.

## Setup status (M6)
- **Real bug fixed** (found via user bug report + a live debugging session,
  not caught by any automated test): `BlanksRoot` and `HintButtonsRoot`
  both had `HorizontalLayoutGroup.childControlWidth/childControlHeight`
  false (only `childForceExpandWidth/Height` was true). Without child-size
  control, tiles/buttons used their own default RectTransform size instead
  of being resized to fit, so long titles (e.g. "The Wizard of Oz") ran off
  the right edge of the screen instead of compressing. Both now set to
  true. **Lesson:** `ForceExpand` alone does not resize children - it only
  weights how leftover space is distributed; `ControlWidth`/`ControlHeight`
  is what actually makes a layout group own the child's size.
- Also fixed along the way (found via the same live debugging session):
  `GameBootstrap.Start()` was throwing `NullReferenceException` on a null
  `config` field with **zero Console output** - Unity's `Awaitable`-
  returning lifecycle methods apparently don't surface an exception thrown
  before the method's first `await`, since nothing ever observes the
  resulting faulted `Awaitable`. A permanent try/catch safety net around
  the whole method body is now in place; apply this same pattern to every
  future `async Awaitable` lifecycle method in this project (`ScreenNavigator`,
  the splash auto-advance, etc.) - this is a proven real footgun, not a
  hypothetical.
- `UITheme` (UI asmdef, `ScriptableObject`): sprite/font/palette fields for
  the upcoming Kenney UI Pack import (M7); asset created at
  `Assets/_Project/Content/UITheme.asset` with fields empty except the
  palette colors (already set to the plan's "campy B-movie theatre" hex
  values). Includes `ApplyButton`/`ApplyTile` helpers ready for M7 to call
  once sprites are assigned - falls back to the current plain look until then.
- 37 EditMode tests still passing project-wide; no new tests needed (this
  milestone is layout/asset-shell only, no new logic).
- Next is M7: import the Kenney UI Pack, populate `UITheme`, reskin the
  gameplay HUD, add minimal click-SFX via a new `IAudioService`.

## Ad-hoc: cloud build pipeline (Unity Build Automation)
Set up before M7's own on-device verification, since local IL2CPP Android
builds are painfully slow on the dev laptop. GameCI (GitHub Actions) was
tried first and hit a real, current dead end worth remembering:
- `game-ci/unity-request-activation-file@v2` is **deprecated** ("no longer
  supported" per its own workflow output).
- Unity has **fully removed manual license activation for Personal
  licenses** from `license.unity3d.com/manual` - not hidden-but-workable
  via a browser trick (which used to be the documented workaround), just
  gone. The portal now says to activate via signing into Unity Hub instead.
- `unity-builder@v4` with only `UNITY_EMAIL`/`UNITY_PASSWORD` (no
  `UNITY_LICENSE`) fails immediately with "Missing Unity License File and
  no Serial was found" - so credential-only activation is **not** actually
  sufficient for Personal despite some (outdated) blog posts claiming v4
  no longer needs the `.ulf` workaround. This resolved a real, repeated
  ambiguity in GameCI's own docs across multiple fetches.
- **Given both the deprecated action and the portal removal, GameCI /
  GitHub Actions is currently a dead end for a Unity Personal license.**
  Pivoted to **Unity Build Automation** (Unity's own first-party cloud
  build service, `cloud.unity.com` - DevOps > Build Automation) instead,
  since it activates against the same Unity ID you're already signed into,
  sidestepping the whole license-file problem entirely.
- Setup: connect GitHub as source control (installs a Unity GitHub App
  scoped to the repo), create an Android build target (**Target setup**,
  not "Quick" - Quick doesn't expose the APK vs AAB choice), Linux builder,
  Unity version `6000.3.19f1`, format APK. Android signing: **auto-generate
  debug key** for now - fine for sideload testing, but a real Play Store
  submission (M15) needs a proper release keystore generated once and kept
  forever, not a fresh debug key each time.
- Free tier requires starting a trial first (no payment details asked) -
  confirmed via 100 free Linux build-minutes/month.
- Repo: `github.com/darkglobe87/bad-movie-clues` (private). No GameCI
  workflow files remain in the repo - removed once the Build Automation
  path was confirmed working.
- **Real bug found via the first successful on-device build, unrelated to
  the build pipeline itself:** `GameHud.SetStatus` (called from the
  `Won`/`Lost` events, which fire synchronously from inside
  `PuzzleState.Guess()`) never called `RefreshBlanks()` - so the letter
  that just won the round never visually appeared in the blanks row; the
  player just saw "YOU WIN!" over an incomplete word. `OnLetterPressed`
  skips its own `RefreshBlanks()` call once the puzzle is over, on the
  (wrong) assumption that `SetStatus` already handled it. Fixed by calling
  `RefreshBlanks()` as the first line of `SetStatus`. Caught only because
  the owner actually played the real cloud-built APK on a real device -
  this exact bug would not have been caught by any EditMode test, since it
  requires observing rendered UI, and the M3-era GameController-direct
  smoke tests never exercise `GameHud` at all.

## Setup status (M7)
- Downloaded the real [Kenney UI Pack](https://kenney.nl/assets/ui-pack)
  (CC0, free, verified current) directly via `curl`, extracted, and
  hand-picked 3 sprites from the **Grey** variant (neutral, so our own
  palette tint via `Image.color` reads true instead of mixing with a
  pre-saturated color) into `Assets/_Project/Content/UI/`: `button_normal.png`
  (192x64, used for both hint buttons and keyboard keys), `tile.png` (64x64,
  blanks-row tiles), `panel.png` (192x64, for future menu screens). License
  kept alongside as `Kenney-UI-Pack-License.txt`. The pack also ships basic
  click sounds - `click.ogg` reused directly for M7 rather than pulling in a
  separate audio pack.
- Sprites imported with `TextureImporterType.Sprite` + a 9-slice
  `spriteBorder` (20px for the 192x64 sprites, 14px for the 64x64 tile) so
  they scale to any button/tile size without corner distortion.
- `IAudioService`/`SimpleAudioService` (Services): deliberately minimal -
  `PlayOneShot(AudioClip)` + an `Enabled` flag, no AudioManager/mixer.
  Uses `AudioSource.PlayClipAtPoint` so it stays a plain C# POCO (no
  MonoBehaviour host needed) per Golden Rule 1.
- `GameHud.Bind` signature changed to `Bind(GameController, IAudioService)`;
  `GameBootstrap` now constructs a `SimpleAudioService` alongside its other
  app-lifetime-ish services and passes it through. `PlaySquish` (previously
  `static`) is now an instance method so it can trigger the click sound -
  every existing call site (all letter keys + all three hint buttons)
  gets the click sound for free with no other changes, since they already
  all routed through `PlaySquish`.
- **Blanks-row tiles restructured**: previously a bare `GameObject` with only
  a `TextMeshProUGUI` (no background at all). Now each tile is a root
  `GameObject` with `Image` (the themed background) plus a child `Label`
  `GameObject` holding the TMP text stretched to fill - same parent/child
  pattern the keyboard keys already used. `PlayPop` now animates the tile
  **root** (so the background pops too, not just the text) - required
  splitting the old `_tileLabels` array into separate `_tileLabels` (for
  `.text`) and `_tileRoots` (for the pop animation) arrays.
- Confirmed by direct code read before touching anything: `PlaySquish`/
  `PlayPop` only ever touch `transform.localScale` - reskinning `Image`
  sprites/colors was guaranteed not to interfere with any existing tween,
  and it didn't.
- Every theme-application call site guards `if (theme != null)` - if the
  `UITheme` reference were ever unassigned, the game degrades to the old
  plain-white look rather than throwing (matches the project's established
  "don't hard-crash on a missing optional reference" instinct).
- 37 EditMode tests still passing; this milestone doesn't touch
  Puzzle/Economy/Core logic at all, so nothing new needed testing there.
- Next is M8: split `GameBootstrap` into a persistent `AppRoot` +
  `GameplayBootstrap`, add `ScreenNavigator`, create `MainMenu.unity` -
  the riskiest milestone in this pass per the plan, isolate it.

## Setup status (M8)
- `AppRoot` (UI asmdef): persistent (`DontDestroyOnLoad`) composition root
  for app-lifetime services (`ISaveService`, `ICurrencyService`,
  `GameConfig`, `IContentProvider`, `IAudioService`). Constructed via
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` rather than being
  placed in a scene - this means it exists before ANY scene's own Awake/
  Start runs, regardless of which scene happens to be first. Deliberate
  choice: M10 will insert a `Splash` scene ahead of `MainMenu` as the new
  first scene, and this way nothing about `AppRoot` needs to move/be
  re-wired when that happens.
- Because `AppRoot` is constructed purely in code (no scene placement), it
  can't have a `[SerializeField]` reference to `GameConfig` wired via the
  Inspector like the old `GameBootstrap` did - so `GameConfig.asset` moved
  to `Assets/_Project/Content/Resources/GameConfig.asset` and is loaded via
  `Resources.Load<GameConfig>("GameConfig")` instead (same mechanism
  `StarterCatalog.json` already used). Verified this resolves correctly
  post-move via a throwaway script before trusting it - would have been
  the exact same failure mode as the M6 null-`config` bug otherwise.
- `ScreenNavigator` (UI asmdef): also spun up by `AppRoot`'s bootstrap
  method, same persistent GameObject tree. Wraps `SceneManager.LoadSceneAsync`
  with a simple black-fade `CanvasGroup` (a `sortingOrder: 1000` overlay
  canvas it builds itself, so it always draws on top of scene-local
  canvases) and the same Awaitable try/catch safety net as `GameBootstrap`.
  **Real API gotcha avoided by verifying first, not guessing:** confirmed
  via Unity's own docs that `SceneManager.LoadSceneAsync` returns a plain
  `AsyncOperation`, not an `Awaitable` - Unity 6 did *not* add an
  Awaitable-returning overload for scene loading, unlike some other APIs.
  Used the same manual `while (!operation.isDone) await Awaitable.NextFrameAsync();`
  polling pattern `BundledContentProvider` already used, rather than
  assume an unverified bridge existed. Separately confirmed PrimeTween
  `Tween`s genuinely are directly awaitable (`await Tween.Alpha(...)`) -
  that one *did* work as hoped.
- `GameBootstrap` (renamed from constructing its own services to pulling
  them off `AppRoot.Instance`) - **kept the class name unchanged**
  rather than renaming to `GameplayBootstrap` as the plan suggested,
  specifically to avoid the risk of a MonoBehaviour class rename breaking
  the scene's existing serialized component reference, in a milestone
  explicitly flagged as "isolate the risk." The functional split (thin
  per-scene bootstrap pulling from a persistent root) is what actually
  mattered, not the name.
- `NavigateButton` (new, tiny, reusable): a button + a target scene name,
  calls `ScreenNavigator.Instance.LoadScene(targetScene)` on click. Used
  for both directions (Play: MainMenu -> Gameplay, Back: Gameplay ->
  MainMenu) rather than writing two near-identical single-purpose scripts.
- `Bootstrap.unity` renamed to `Gameplay.unity` via `AssetDatabase.RenameAsset`
  (not a raw filesystem move) specifically to preserve its GUID - verified
  directly by diffing the `.meta` file's guid before/after the rename
  rather than assuming the rename tool behaved safely. `MainMenu.unity`
  created fresh: Canvas, EventSystem, a title label, and one placeholder
  "PLAY" button - deliberately plain/unstyled, since the real menu UI
  (Level Select, Store, Settings, actual layout) is M10's job, not this
  milestone's. Same placeholder-quality "< Menu" button added to a corner
  of `Gameplay.unity`. Build Settings updated: `MainMenu.unity` first,
  `Gameplay.unity` second.
- **Verification gap, worth being honest about:** the plan called for "an
  EditMode test that AppRoot composes its services exactly once," but
  `[RuntimeInitializeOnLoadMethod]` genuinely does not fire during EditMode
  test execution (EditMode tests never enter Play Mode) - so there's no
  way to exercise the actual bootstrap trigger path in an automated
  EditMode test, discovered only once actually trying to write one. What
  *is* automated: clean compile, all 37 existing tests still green, and a
  direct scripted check that `Resources.Load<GameConfig>` resolves
  correctly post-move. What is **not** automated and needs a human:
  whether `AppRoot`/`ScreenNavigator` actually behave correctly across a
  real scene transition in Play Mode - this milestone's actual core claim
  (services survive `MainMenu` -> `Gameplay` -> `MainMenu`) has not been
  verified by anything but a human pressing Play. **Please open
  `MainMenu.unity`, press Play, click PLAY, click < Menu, click PLAY
  again, and confirm nothing errors and the coin balance shown in
  Gameplay is the same both times you visit it** (it starts at 100 and
  doesn't change unless you spend/earn, so seeing the same number twice is
  the actual proof persistence survived the scene change).
- **Human verification confirmed M8 works**: coin balance persists across
  scene transitions, fade transition looks good.
- **Bug report investigated, turned out not to be a bug**: user reported
  the Character hint button "didn't work" for The Wizard of Oz (the only
  level reachable pre-M11, since it's index 0 and there's no level select
  yet). Traced the entire path end-to-end via a throwaway batch script
  calling `GameController` directly (same pattern as M3/M4): catalog data
  has `characterClue: "Toto"`, `TryRevealCharacterHint()` returns `true`
  and spends exactly 30 coins, clue text sets correctly - the pure-C# path
  is provably correct. **Real cause, confirmed with the user**: the button
  was visibly greyed out, i.e. correctly disabled because their coin
  balance had already dropped below the 30-coin cost from earlier test
  sessions - this is *new* behavior specifically because M8 made the
  balance genuinely persist. A disabled Unity `Button` never fires
  `onClick`, so there was zero feedback explaining *why* - indistinguishable
  from broken. Fixed by making `GameHud.RefreshHintButtons()` rewrite each
  hint button's label live (`"Character (30) - need 12 more"` when
  unaffordable, plain `"Character (30)"` once affordable/revealed) instead
  of setting the label text once in `Bind()` and never touching it again -
  same gating logic as before, just now self-explanatory.
  - **Batch-script gotcha found while writing the throwaway verifier**:
    spin-waiting on an `Awaitable` with `while (!task.IsCompleted)
    Thread.Sleep(10);` on the main thread deadlocks in `-batchmode`.
    `Awaitable` continuations are dispatched off Unity's player loop, and
    blocking the main thread with `Thread.Sleep` prevents that loop from
    ever ticking again - nothing ever completes the task, so the process
    hangs forever with no error. Fix: poll from inside an
    `EditorApplication.update` callback instead (which the editor invokes
    directly, unlike a blocked thread), and call `EditorApplication.Exit`
    from there once done. Apply this pattern to any future throwaway batch
    script that awaits Unity's `Awaitable`.
- **Keyboard changed from alphabetical A-Z to QWERTY**, per user request.
  `GameHud.BuildKeyboard()` now builds three rows (`QWERTYUIOP` /
  `ASDFGHJKL` / `ZXCVBNM`) instead of one flat 26-letter sequence.
  `KeyboardRoot`'s layout component changed from `GridLayoutGroup` (which
  wraps by available width with no row-grouping control) to
  `VerticalLayoutGroup` in `Gameplay.unity`; each row is now a runtime-built
  child `RectTransform` with its own `HorizontalLayoutGroup`
  (`ChildControlWidth/Height = true`, same as the M6 layout-overflow fix -
  `ForceExpand` alone still doesn't resize children). Rows fill the full
  keyboard width regardless of key count, so the 7-key bottom row has
  visibly wider keys than the 10-key top row - intentional, matches how
  real mobile keyboards behave, not a bug.
- All 37 EditMode tests still passing; no test changes needed (both fixes
  are UI-only/scene-only, no logic changed).
- **Not yet verified in Play mode/on-device**: neither the new QWERTY
  layout's visual row spacing nor the new hint-button "need N more" labels
  have been eyeballed by a human. Worth checking alongside M9's dust
  particles rather than a separate check-in.

## Setup status (M9)
- **`AmbientDustBackground`** (UI asmdef, static builder class, not a
  `.prefab` asset): builds a persistent background camera + a Shuriken
  `ParticleSystem` entirely in code, matching `AppRoot`'s own
  no-scene-placement pattern from M8 (hand-authoring a `.prefab` YAML file
  with a fully-configured `ParticleSystem` would be far more error-prone
  than just calling the real Unity API at runtime - same reasoning that
  kept `BuildKeyboard`/`BuildBlanksRow` code-driven). `AppRoot.Initialize()`
  calls `AmbientDustBackground.Build(transform)` and stores the returned
  `ParticleSystem` as `AppRoot.Instance.DustParticles`, so a future
  Settings screen (M10) can call the new
  `AmbientDustBackground.SetReducedEffects(dustSystem, reduced)` hook.
- Params follow the plan's spec directly: World simulation space, 50 max
  particles, ~10/sec emission, 8-14s lifetime, 0.1-0.3 start speed,
  0.15-0.5 start size, warm pale-gold start color, alpha 0→0.35→0 over
  lifetime (fade in/out, never pops), noise strength 0.08/frequency 0.2 for
  gentle wander, gravity modifier -0.02 for a barely-there upward drift.
  Emission shape is a generous 10x14 world-unit box so it comfortably
  covers any phone aspect ratio without per-device tuning.
- **No new sprite asset needed for the first pass**: the plan calls for a
  "soft round particle sprite" sourced as a FREE asset, but Unity's
  built-in default `ParticleSystemRenderer` material already renders a
  soft radial-alpha dot - close enough for an ambient effect, so M9 ships
  with zero new asset files. Swapping in a custom sprite later (once one's
  sourced) is a one-line material change, not a rework.
- Background camera clears to a **solid color** (`#2A1A3E`, the palette's
  gradient-top hex) rather than the actual AI-generated gradient texture
  the plan describes - that asset hasn't been sourced yet. Documented as a
  known follow-up, not a blocker; swapping a solid clear color for a
  gradient quad behind the particles is a self-contained follow-up change.
- **Real conflict found and fixed, not anticipated going in:** both
  `MainMenu.unity` and `Gameplay.unity` had their own default "New Scene"
  `Main Camera`/`Camera` GameObject (`ClearFlags = Solid Color`, unused
  leftover from Unity's scene template - confirmed via direct read that
  neither scene's `Canvas` (`m_RenderMode: 0` = Screen Space Overlay,
  `m_Camera: {fileID: 0}`) ever referenced a camera). Camera render order
  is lowest-depth-first, so the persistent background camera (depth -10)
  would render its gradient + particles *first*, then each scene's own
  Camera (depth 0, Solid Color clear) would immediately paint over it
  every frame - the background would never have been visible. Fixed by
  removing the per-scene camera GameObject from both scenes entirely (via
  a throwaway batch script opening each scene, deleting the
  `MainCamera`-tagged object, and saving) rather than just changing its
  Clear Flags, since it served no other purpose. Each scene's Camera also
  carried the only `AudioListener` in the scene - `AmbientDustBackground`'s
  camera now carries the app's sole (persistent) `AudioListener` instead,
  so removing the scene cameras doesn't silence `SimpleAudioService`.
- 37 EditMode tests still passing; no test changes needed (this milestone
  is rendering/VFX-only, no testable logic).
- **Not yet verified in Play mode/on-device**: particle drift visibility,
  readability of UI over the new background, and frame-rate impact on the
  dev laptop/device all need a human eyeball per the plan's own M9
  verification criteria - none of that can be checked from this
  environment. Worth checking alongside M6-M8's still-unverified QWERTY
  keyboard layout and hint-affordability labels in one combined pass
  rather than separate check-ins.
- Next is M10: main menu screen (Play/Store/Settings buttons, logo), a
  short animated `Splash.unity` scene ahead of `MainMenu`, and a
  `SettingsScreen` (Reduced Effects toggle wired to the new
  `SetReducedEffects` hook, mute toggle, reset progress, credits) backed
  by a new `IUserSettings`/`SettingsService`.

## Bug found on-device: dust particles rendered as hard magenta squares
Reported after the first real on-device build showed the M9 particles as
solid magenta diamonds/squares instead of soft faded dots (see the actual
screenshot in that conversation for reference).
- **Root cause, confirmed by direct reasoning about the symptom, not
  guesswork:** solid magenta with a hard edge is Unity's own "missing
  shader" fallback appearance, not just an unstyled sprite. A
  `ParticleSystem` added via `AddComponent` at runtime (as
  `AmbientDustBackground.Build` does) gets **no material at all** - the
  soft round default only gets assigned when a Particle System is created
  through the Editor's GameObject > Effects menu, which explicitly wires a
  default material as part of that menu command. Whatever fallback Unity
  substituted internally then had its shader **stripped from the IL2CPP
  Android build**, since nothing else in the project referenced it -
  hence "no shader" at runtime, hence the magenta error color (also
  explains why the color was hot pink instead of the configured pale
  gold: the tint was never being applied by a real shader in the first
  place).
- **Fix:** a real, committed Material asset instead of relying on any
  Unity-internal default. `Assets/_Project/Content/VFX/dust_particle.png`
  is a 128x128 soft radial-alpha white dot (generated once via a
  throwaway Editor script - `Texture2D.SetPixel` per-pixel distance
  falloff, `EncodeToPNG`, saved as a real file - consistent with "images
  shipped as files," just authored by a one-time tool run instead of an
  external image editor, matching how the M1 authoring tool produces
  ordinary content). `Assets/_Project/Content/VFX/Resources/
  DustParticleMaterial.mat` uses the **`Sprites/Default`** shader (chosen
  over `Particles/Standard Unlit` after actually probing which shaders
  `Shader.Find` could resolve, rather than guessing a name upfront -
  `Sprites/Default` won because it's simple, alpha-blends out of the box,
  respects vertex color, and needs no keyword/mode dance the way
  `Particles/Standard Unlit`'s custom ShaderGUI does). `AmbientDustBackground.Build`
  now loads it via `Resources.Load<Material>("DustParticleMaterial")` and
  assigns it to the `ParticleSystemRenderer` explicitly.
- **Extra safety net:** `Sprites/Default` was already present in
  ProjectSettings' "Always Included Shaders" list (checked programmatically
  via `SerializedObject` before assuming), which is exactly the setting
  that would prevent this exact class of bug (a shader with no static
  scene reference getting stripped from the build) from recurring for
  this material specifically. Worth checking this list any time a
  runtime-only (no scene reference) shader/material is introduced.
- 37 EditMode tests still passing; this was a rendering-only fix.
- **Still not verified on-device** - the underlying premise (shader
  stripping was the cause) is reasoned from the symptom and Unity's known
  behavior, not confirmed by a second on-device build yet. Worth a fresh
  cloud build + install to confirm the particles now render as soft faded
  dots before considering this fully closed.
- **Scope clarified with the user**: a comment about wanting "full 3D UI
  conversion" turned out to mean wanting the existing 2D UI to *look* more
  premium (depth, bevels, parallax, polish), not an actual architecture
  change to 3D meshes/world-space UI. Confirmed via AskUserQuestion -
  staying on uGUI + TMP, no rework planned. Worth keeping this in mind
  through M10 onward: lean into depth cues (drop shadows, layered
  parallax, juicier motion) within the existing 2D system rather than
  treating this as a separate milestone.
- **Confirmed on-device the particle fix worked** ("the background looks
  fantastic") - closes out the open verification gap from above.
- **Follow-up readability bug, same root cause class as M9's particles**:
  `descriptionText`, `coinBalanceText`, and `characterClueText` all had
  `m_fontColor` hardcoded to black in the scene, dating from before M9's
  real dark-purple background existed - black-on-`#2A1A3E` was "almost
  unreadable" once the actual background shipped. Tile and keyboard-key
  labels were correctly left black (they sit on `NeutralLight`-colored
  tile/key sprites, where black *is* the right contrast choice) - only
  the three text elements sitting directly on the bare background needed
  a change. Fixed in code, not by hand-editing the scene YAML again:
  `GameHud.Bind()` now sets all three to `theme.NeutralLight` (`#F5ECD9`,
  the palette's "warm paper" color, already defined in `UITheme` for
  exactly this purpose) inside the existing `if (theme != null)` guard,
  matching the established pattern rather than adding a new one.
- User feedback, deferred rather than acted on now: wants description-text
  styling and UI elements that visually match the "badly drawn artwork"
  clue-image aesthetic eventually - explicitly said to keep going with the
  planned milestone sequence for now rather than open a dedicated styling
  pass. Worth revisiting once more of the M10-M13 UI exists to style as a
  batch.
- 37 EditMode tests still passing; this was a color-only fix.

## Setup status (M10)
- **`IUserSettings`/`SettingsService`** (Services asmdef): `ReducedEffects`
  and `AudioEnabled` bools, save-backed via the existing `ISaveService`,
  `Changed` event - directly mirrors `ICurrencyService`/`CurrencyService`'s
  shape (same constructor pattern, same "only fire event on an actual
  change" behavior). 5 new EditMode tests, same style as
  `CurrencyServiceTests`.
- **`ICurrencyService.Reset(int)` added** (small, deliberate scope
  addition): "Reset Progress" in Settings needs *something* to reset, but
  `IProgressService` doesn't exist until M11 - resetting the coin balance
  is the only persisted state that exists right now. Belongs on
  `ICurrencyService` since it's currency-domain logic, not something
  `SettingsScreen` should reach past the interface for. 2 new EditMode
  tests. Once M11 ships `ProgressService`, "Reset Progress" should also
  clear solved/unlocked level state - noted in `SettingsScreen`'s summary
  comment so it isn't forgotten.
- **`AppRoot`** now also owns `Settings` (`IUserSettings`), constructed
  last in `Initialize()` since applying it needs `AudioService` and
  `DustParticles` to already exist - `ApplySettings()` pushes
  `Settings.AudioEnabled`/`Settings.ReducedEffects` onto them once at
  startup and again on every `Settings.Changed`.
- **`MainMenuScreen`, `SettingsScreen`, `SplashScreen`** (UI asmdef): all
  three build their entire UI procedurally at runtime (RectTransform
  anchors, `AddComponent` calls), the same pattern `GameHud.BuildKeyboard`/
  `BuildBlanksRow` already established - not a new decision, just applying
  the existing one consistently, and still the only real option without
  Editor GUI access to hand-place UI in this environment.
  - `MainMenuScreen`: title + Play/Store/Settings button row. Store is
    present but non-interactable with a "(coming soon)" label (M12's job) -
    same self-explanatory-disabled-state pattern as the hint buttons'
    "need N more" labels from the M8 bugfix, not a new pattern.
  - `SettingsScreen`: Reduced Effects toggle, Mute toggle (inverted from
    `AudioEnabled` - the toggle reads "Mute", the underlying bool is
    "enabled"), Reset Progress, Restore Purchases (disabled/"coming soon",
    same reasoning as Store - `IPurchaseService` is M12), Credits (opens a
    small inline text panel with a Close button, not a separate screen),
    version number (`Application.version`), Back. Toggles are hand-built
    from `Image`+`Toggle`+child `Checkmark` `Image` rather than a prefab,
    since nothing like a themed toggle sprite exists yet - functional but
    plain; a real themed toggle asset is future polish, not blocking.
  - `SplashScreen`: logo pop-in (`PrimeTween Ease.OutBack` from zero
    scale), tagline fade-in, ~2s auto-advance to `MainMenu`, tap-anywhere-
    to-skip. Requires an `Image` on its own `GameObject`
    (`[RequireComponent(typeof(Image))]`) sized full-screen with alpha 0 -
    `IPointerClickHandler` only fires where a raycastable `Graphic` is
    actually hit, so a bare `MonoBehaviour` with no `Graphic` anywhere in
    its hierarchy would never receive the tap at all. Same Awaitable
    try/catch safety net as every other async lifecycle method in this
    project (M6 lesson, keeps getting reapplied as new ones are added).
- **`Splash.unity`** created fresh (Canvas, EventSystem, `SplashScreen`) and
  **Build Settings reordered**: `Splash` (0) → `MainMenu` (1) → `Gameplay`
  (2). `AppRoot`'s `RuntimeInitializeOnLoadMethod` construction (M8) was
  specifically designed to not care which scene loads first, so this
  insertion needed zero changes to `AppRoot` itself - confirms that M8
  design bet paid off.
- **`MainMenu.unity` rebuilt**, not hand-edited: a throwaway batch script
  opened the scene, destroyed the old M8 placeholder children
  (`TitleText`, `PlayButton`), added a `MainMenuScreen` component, and
  wired its `theme`/`clickSound`/`canvasRoot` serialized fields via
  `SerializedObject` (matching the established "use the real API, not
  hand-crafted YAML" preference from earlier milestones' GUID lessons).
- 44 EditMode tests passing (37 previous + 7 new: 5 `SettingsService`, 2
  `CurrencyService.Reset`). Clean project-wide compile is also confirmed
  correct for `MainMenuScreen`/`SettingsScreen`/`SplashScreen` themselves,
  since a compile error anywhere would have failed the whole test run.
- **Not yet verified in Play mode/on-device** - same category as every
  previous milestone's UI. Specifically needs a human to: watch the splash
  animate and auto-advance (or tap to skip), confirm Play/Settings buttons
  work and Store shows correctly disabled, open Settings and toggle both
  switches, hit Reset Progress and confirm the coin balance actually drops
  back to `StartingBalance`, open/close Credits, confirm the version
  number renders, and confirm Back returns to the button row correctly.
  None of this can be checked from this environment - worth one combined
  Play-mode/device pass covering all of it.

## Bug found on-device: Settings screen toggles rendered as giant boxes
Reported with a screenshot showing the Reduced Effects/Mute toggles as
near-full-height white/green rectangles overlapping the title text.
- **Root cause**: `SettingsScreen`'s toggle rows and buttons set
  `RectTransform.sizeDelta` directly, but their parent `VerticalLayoutGroup`
  has `childControlHeight = true` - in that mode the layout group computes
  and *overwrites* each child's size from preferred/min values, silently
  ignoring `sizeDelta` entirely. This is the same underlying rule as the M6
  layout bug ("`ControlWidth`/`ControlHeight` owns the size, not
  `sizeDelta`/`ForceExpand`"), just biting a second time in the specific
  case of mixed fixed-and-flexible-height rows, which M6's fix never
  exercised. Fixed by giving every sized element (toggle rows, the toggle
  box itself, buttons, the version text) an explicit `LayoutElement` with
  `preferredHeight`/`preferredWidth` (and matching `minHeight`/`minWidth`)
  instead of touching `sizeDelta` at all once a `ControlHeight`-enabled
  ancestor is involved. **Lesson to actually generalize this time**: any
  RectTransform inside a `ControlWidth`/`ControlHeight` layout group needs
  a `LayoutElement` if it isn't meant to just equally share space via
  `ForceExpand` - `sizeDelta` on such a child is not just unreliable, it's
  flatly ignored.
- **Second bug, same screenshot**: the checkmark showed solid green (the
  no-theme fallback color), meaning `UITheme` genuinely wasn't reaching
  `SettingsScreen`. Confirmed by direct scene inspection -
  `MainMenuScreen`'s `theme` field was `{fileID: 0}` (null) despite
  `clickSound` and `canvasRoot` - set via the exact same
  `SerializedObject`/`ApplyModifiedProperties()` call in the same batch
  script - wiring correctly. Reproduced deliberately in a follow-up
  diagnostic script: assigning `SerializedProperty.objectReferenceValue`
  for this specific field silently did not persist even after
  `ApplyModifiedProperties()` + re-reading it back in the same run: cause
  not fully root-caused (a genuine, narrow Unity batch-mode quirk, not a
  logic bug in this codebase), but reliably worked around by falling back
  to direct reflection (`FieldInfo.SetValue` + `EditorUtility.SetDirty`)
  when the `SerializedObject` path doesn't stick - verified by reading the
  value back before saving, not just assuming success. Worth reaching for
  this reflection fallback immediately if a `SerializedObject` field
  assignment for a *ScriptableObject* reference (as opposed to the
  `AudioClip`/`Component` references that worked fine) doesn't show up in
  the saved scene again.
- **Third bug, found while rewriting, not from the report**:
  `SettingsScreen.Refresh()` set the mute toggle from `_settings
  .AudioEnabled` directly, but the toggle displays "Mute" (the *inverse* of
  `AudioEnabled`) - would have shown backwards every time Settings was
  reopened after the first toggle. Fixed alongside the layout rewrite.
- Also fixed: `MainMenuScreen` never hid the title text when Settings
  opened, so it stayed visible behind/through the settings panel - now
  toggled alongside the button panel.
- 44 EditMode tests still passing; all three fixes were UI/wiring-only.
- **Still not verified on-device** - needs a fresh build to confirm the
  toggles now render as properly-sized boxes and the theme (button
  sprites, gold checkmark) actually shows.
- **User asked how to reach a more "highly themed" look** (referencing
  Kenney-style asset-pack mockups with bordered buttons, panel headers,
  icon sets, and a bold outlined/shadowed font) - answered inline, not yet
  actioned: import more of the already-downloaded Kenney UI Pack (toggle/
  checkbox sprites, icon set, panel-header variant - M7 only pulled 3 of
  many available sprites) and source one free rounded display font (e.g.
  Fredoka/Baloo/Bungee) as a TMP SDF asset with Outline+Underlay material
  settings for the bold-with-shadow text look. Deliberately deferred as a
  dedicated styling pass once M10-M13's screens all exist, rather than
  restyling incrementally - matches the earlier "keep going with the
  planned milestone sequence for now" preference from the readability-fix
  conversation. `UITheme.HeadingFont`/`BodyFont` fields already exist
  (M6) and are still empty, waiting for this pass.

## Setup status (M11)
- **New `BadMovieClues.Progression` asmdef** (plain C#, references only
  `BadMovieClues.Services`), matching the plan's explicit call-out. Added
  as a reference to `Core` (needs it for `GameController`), `UI` (needs it
  for `AppRoot`/`LevelSelectScreen`), and `Tests`.
- **`IProgressService`/`ProgressService`**: tracks solved level ids
  (`HashSet<string>`, persisted as a `string[]` DTO like
  `SettingsService`'s pattern) and `HighestUnlockedIndex`. `MarkSolved`
  unlocks `index + 1` and only fires `Changed`/persists if something
  actually changed (`Add` returning false or the highest-unlocked value
  not increasing correctly no-ops) - covered by a test that solves an
  already-solved level twice and one that solves an out-of-order later
  level first, confirming an earlier solve afterward doesn't lower
  progress. `unlockAllForTesting` constructor flag per the plan's own
  spec, for dev testing without grinding through 36 levels. `Reset()`
  added so Settings' "Reset Progress" can clear this alongside the coin
  balance now that both exist.
- **`GameController` gained `CurrentIndex` and an `IProgressService`
  constructor dependency** - `LoadLevelAsync` now records which catalog
  index actually loaded (post-modulo), and the `PuzzleState.Won` handler
  calls `_progressService.MarkSolved(level.Id, capturedIndex)` before
  re-firing its own `Won` event. `GameBootstrap` passes `app.Progress`
  through and now loads `app.SelectedLevelIndex` instead of a hardcoded
  `0`.
- **`AppRoot.SelectedLevelIndex`** (plain settable int, default 0): the
  simplest way to pass "which level was picked" across a scene reload -
  `LevelSelectScreen` sets it before navigating to `Gameplay`,
  `GameBootstrap` reads it. Matches the established "AppRoot is the single
  source of truth for cross-scene state" pattern rather than extending
  `ScreenNavigator.LoadScene` with a payload parameter for a single use
  case.
- **`LevelSelectScreen`/`LevelCard`**: a `ScrollRect` + `GridLayoutGroup`
  (3 columns, procedurally populated, same code-driven pattern as every
  other screen) over all 36 catalog movies. **Locked cards deliberately
  don't show the movie title** - just the level number and "Locked" - so
  browsing the grid doesn't spoil puzzles you haven't unlocked yet; this
  wasn't explicitly called out in the plan but follows directly from what
  the game actually is (a guessing game - the title is the answer).
  Solved cards get a checkmark suffix. `MainMenuScreen.Start()` became
  `async void` (same justified exception to "no async void" as
  `SplashScreen` - it's a top-level Unity lifecycle method with no caller
  to await, same try/catch safety net) to await
  `IContentProvider.LoadCatalogAsync()` once and hand the resolved
  `LevelCatalog` to `LevelSelectScreen.Init()` synchronously, rather than
  giving each panel-open its own async build step.
- **PLAY now opens Level Select instead of jumping straight into
  Gameplay** - direct consequence of Level Select existing; going straight
  to a level no longer makes sense once there's a real chooser.
  `MainMenuScreen`'s panel-closed callback was generalized from
  `OnSettingsClosed` to `OnPanelClosed` since both Settings and Level
  Select now return to the same button row the same way.
- 51 EditMode tests passing (44 previous + 7 new `ProgressServiceTests`).
- **Not yet verified in Play mode/on-device** - same category as every
  previous milestone. Specifically: solve a level, confirm the next
  unlocks in the grid and the checkmark appears, confirm locked cards
  genuinely hide the title, confirm Reset Progress in Settings actually
  re-locks everything, and confirm progress persists after fully closing
  and reopening the app (not just navigating scenes within one session).

## Bug found on-device: images not loading, inconsistent hint charging
Reported after testing Level Select on a real device: clue images never
appeared for any level, some levels (e.g. The Matrix) charged the picture
hint's coin cost with nothing to show for it, other levels didn't charge
at all when the picture hint was pressed, and Reset Progress only reset
coins, not solved/unlocked levels.
- **Root cause of the image bug, empirically confirmed, not just
  theorized:** `BuildPipeline.BuildPlayer` never built Addressables
  content first. Editor Play mode never needed this - it reads assets
  directly from the project, no bundle required - which is exactly why
  every prior milestone's Editor-based verification (M3's smoke test
  loading the "jaws" level, every subsequent Play-mode check) saw images
  load fine and never caught this: a real device build has nothing to
  load from without an explicit `AddressableAssetSettings
  .BuildPlayerContent()` step, and `Addressables.LoadAssetAsync` just
  returns null for a key that's registered but never actually packed into
  a bundle - same *class* of gap as the M9 particle-shader-stripping bug
  (something that only matters for a real build, invisible from the
  Editor), different mechanism. Verified directly rather than assumed: a
  throwaway script confirmed all 7 image entries (`img_matrix` included)
  are correctly registered in the `BadMovieClues-Images` Addressables
  group, and that calling `BuildPlayerContent()` directly succeeds with no
  error and produces a real catalog file on disk.
- **Fix**: new `AddressablesBuildPreprocessor`
  (`Assets/_Project/Scripts/Editor/`, in the proper `BadMovieClues.Editor`
  asmdef, not a throwaway) implementing `IPreprocessBuildWithReport` -
  fires automatically before *any* player build, local or cloud. Unity
  Build Automation runs the same underlying build pipeline a local build
  does, so this fires there too with zero extra configuration needed in
  the Build Automation UI - no per-build manual step to remember.
- **Also fixed while in there**: `BuildAndroidTest.cs` (the permanent
  local test-build script) still pointed at `"Bootstrap.unity"`, a scene
  renamed to `Gameplay.unity` back in M8, and only ever built that one
  scene - now builds every scene actually in Build Settings
  (`Splash`/`MainMenu`/`Gameplay` as of M10), matching what a real build
  needs.
- **Real UX bug, not just a symptom of the Addressables gap**: the Picture
  hint button charged its coin cost on *any* level regardless of whether
  that level actually had a loadable image - 29 of the 36 catalog movies
  have no `imageKey` at all yet, so pressing Picture on any of those
  spent coins for literally nothing. `GameHud.RefreshHintButtons` now
  gates on `_pendingPictureSprite != null` (not just a non-empty
  `ImageKey` string, so it also protects against a future/residual load
  failure) - the button is simply non-interactable with a "Picture (no
  image)" label on levels with nothing to show, matching the established
  self-explanatory-disabled-state pattern.
- **Reset Progress relocking**: re-verified by direct code read that
  `SettingsScreen.OnResetProgress` (now `OnResetConfirmed`) already calls
  both `_currency.Reset(...)` *and* `_progress.Reset()` - this was
  correctly wired in the M11 commit already pushed. The report is most
  likely a stale build (tested before the M11 push had actually been
  pulled into a fresh cloud build) rather than a new bug - flagging this
  honestly rather than claiming a fix for something that was already
  correct in source.
- **Added anyway, genuinely missing**: a confirmation popup before Reset
  Progress executes (`SettingsScreen.BuildResetConfirmPanel` - inline
  panel with Cancel/Reset buttons, same pattern as the Credits panel) -
  previously a single misclick would silently wipe all progress with no
  way back.
- **Two feature requests deferred, not implemented now** (per explicit
  instruction to fix bugs and move straight to M12): a "go to next level"
  button, which M13's plan already covers (`LevelCompleteScreen`'s Next
  button); and a star completion rating that loses a star per hint type
  used - genuinely new scope, noted as an addendum on M13's prompt in the
  plan file rather than half-implemented ahead of time.
- 51 EditMode tests still passing; all fixes were build-pipeline/UI-only,
  no puzzle/economy logic changed.
- **Not yet verified on-device** - needs a fresh cloud build (which will
  now include the Addressables preprocessor automatically) to confirm
  images actually load, the no-image gating reads correctly, and the
  confirmation popup appears before Reset Progress executes.


## Setup status (M12)
- **`IPurchaseService`/`CoinPack`** (Services asmdef, per the plan) - a
  purchase confirms success or failure only; it deliberately knows nothing
  about coins/currency. Three fixed packs (500/1200/3000 coins) - a full
  `StoreConfig` ScriptableObject felt like overkill for 3 hardcoded values
  with no per-pack tuning need yet, unlike `GameConfig`'s hint costs which
  actually get tuned.
- **`StubPurchaseService` deliberately lives in `Economy`, not
  `Services`**, despite the plan grouping it there with `SettingsService`/
  `SimpleAudioService`. It needs `ICurrencyService` to grant coins on a
  successful purchase, and `Economy` already references `Services` (not
  the reverse) - putting the concrete class in `Services` would require
  `Services -> Economy`, inverting/circularizing the established
  dependency direction. Same class of deviation as `GameBootstrap` living
  in `UI` instead of `Core` (M3): the interface stays where the plan put
  it, the concrete implementation goes wherever the dependency graph
  actually allows it to compile - Golden Rule 4 calls this out explicitly
  as the correct response, not an asmdef reference to force through.
- **First `[UnityTest]`-based tests in this project**:
  `PurchaseAsync`/`RestorePurchasesAsync` return `Awaitable<bool>`, and
  every previous service (`CurrencyService`, `SettingsService`,
  `ProgressService`) was fully synchronous, so nothing needed to await
  anything yet. `StubPurchaseServiceTests` uses `[UnityTest]` returning
  `IEnumerator`, pumping `purchases.PurchaseAsync(id).GetAwaiter()` via
  `while (!awaiter.IsCompleted) yield return null;` - same underlying
  "don't block the thread that's supposed to drive completion" lesson as
  the M8 batch-script `Thread.Sleep` deadlock, applied here through the
  test framework's own coroutine pumping instead of a blocked spin-wait.
- **`StoreScreen`**: coin-pack buttons, a live balance display, Restore
  Purchases (stub), Back - built procedurally, same
  `LayoutElement`-for-fixed-sizing approach as `SettingsScreen` applied
  from the start this time (not re-discovered the hard way). `MainMenuScreen`'s
  Store button is real now, not the M10 "(coming soon)" placeholder -
  `OnPanelClosed` extended to also hide `StoreScreen`.
- 55 EditMode tests passing (51 previous + 4 new
  `StubPurchaseServiceTests`).
- **Not yet verified in Play mode/on-device** - same category as every
  previous milestone. Specifically: buy a pack, confirm the balance rises
  by exactly the pack amount and the display updates live, confirm
  Restore Purchases doesn't error with nothing to restore, confirm
  purchased coins persist after returning to the button row and
  navigating away.
- Next is M13: `ConfettiBurst` + `LevelCompleteScreen` (win/lose
  celebration, Next button, coins-earned count-up) - the last milestone in
  the M6-M13 Visual/Art/UX pass before M14 (remote content) and M15
  (Android + real SDKs). Also where the user-requested star-rating idea
  (noted as an addendum on this milestone's prompt in the plan file,
  flagged during M11's bugfix pass) should be scoped in detail rather than
  left as a note.
