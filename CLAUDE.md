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
- Next is M9: build the dust-particle ambient background as a prefab on
  `AppRoot`, so it renders behind every scene's UI with no duplication.
