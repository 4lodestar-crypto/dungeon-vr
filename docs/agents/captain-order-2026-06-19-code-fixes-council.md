# Code Fixes for DungeonGenerator & DungeonParams — Council Findings

**From:** Halk (Captain)
**To:** gameplay-systems specialist
**Priority:** High — affects Phase 3 stability

---

Fix these 3 bugs discovered by council code review. All files in `C:\Projects\DungeonVR\Assets\Scripts\`.

## Bug 1: [CRITICAL] `GenerateAndLoad` leaks temp GameObject on success
**File:** `Assets/Scripts/Level/Logic/DungeonGenerator.cs`, lines ~171-182

The method creates `new GameObject("DungeonGenerator_Loader")`, adds a LevelLoader, and destroys it only on failure. On success the GameObject leaks into the scene.

**Fix:** Destroy the temp object after successful load too. Either:
- `DestroyImmediate(loaderObj)` before `return true` on line 182, or
- Wrap in `try/finally` to guarantee cleanup

## Bug 2: [HIGH] `TryGenerate` throws `ArgumentOutOfRangeException` on small grids with large rooms
**File:** `DungeonGenerator.cs`, lines 270-271

`rng.Next(2, width - rw - 2)` throws when `width - rw - 2 <= 2`.
Same for `depth` variant on line 271.

**Root cause:** `DungeonParams.Clamp()` doesn't constrain `MaxRoomSize` relative to `Width`/`Depth`.

**Fix (two parts):**
1. In `DungeonGenerator.TryGenerate()` (or nearby), clamp `rw` and `rh` so `width - rw - 2 > 2` and `depth - rh - 2 > 2`. If clamping makes placement impossible, skip the room.
2. In `DungeonParams.Clamp()`, add: `_maxRoomSize = Mathf.Min(_maxRoomSize, _width / 2, _depth / 2);`

## Bug 3: [MEDIUM] `ValidateGenerated` always passes `null` palette → always returns `false`
**File:** `DungeonGenerator.cs`, line ~205

`validator.Validate(tiles, ..., null, out ...)` — passing `null` palette means the validator always reports "Tile palette is null." and validation fails.

**Fix:** `ValidateGenerated()` doesn't need palette validation (it validates the tile layout, not asset references). Either:
- Change `LevelValidator.Validate()` to skip palette check when palette is null, or
- Pass a default/dummy palette, or
- Create overload `ValidateGenerated` that calls a tiles-only validation method

## Apply & Verify
1. Fix all 3 bugs
2. Run the existing 8 EditMode tests to verify no regressions
3. Verify the tests still pass

Report back with the commit SHA when done.

---

*— Halk*
