# Highway three-lane rebuild — execution record

## Authorization and reference

The user explicitly confirmed **actual game-scene implementation**, replacing the earlier planning-only restriction for this task. Reference: `http://127.0.0.1:8776/three-lanes/native/` and its native Unity screenshot. Keep existing assets, gameplay camera and HUD. Three playable lanes, white separators, median with a separate empty background road. All moving Highway vehicles must approach against player travel. Earlier five proposals remain rejected.

## Work scope

1. Persist route-following three-lane paint, median and an empty adjacent road across HighWay; preserve its current route, chapter connection and recovery forks.
2. Configure all three Highway traffic lanes head-on. Avoid three-lane arrival walls and simultaneous mandatory encounter/traffic conflicts. Preserve RestStop's authored behavior through opt-in settings.
3. Remove Highway rear-chase car events and make Highway log-truck events approach head-on; retain other authored combat and rewards. Clear bypass scenery intersections and keep road corridors unobstructed.
4. Verify saved-scene contracts, focused runtime rules, native visual captures, actual driving and repeated startup/exit. Preserve and restore user preferences/test controls.

## Boundaries

No replacement character assets, new paid generation, campaign economy redesign, stage-length change, or broad RestStop rewrite. Preview enemy hiding/health freezing is not part of the installed scene. No worktree/branch reset or automatic commit of unrelated existing edits.

## Evidence and recovery

- Branch: `fix/mobile-combat-ui-performance` (existing dirty workspace retained).
- Pre-change source/scene copies and hashes: `outputs/highway-three-lane-rebuild-2026-09-26/before/`.
- Edit the live scene only through official Unity CLI/Pipeline; no raw scene YAML edits.
- HighWay started clean in the live Editor; its file already had user changes relative to Git and was backed up as-is.

## Status

- Discovery and working-copy backup complete.
- Runtime lane/direction rules, native installation and saved-scene reopen completed.72enemies and29bonus pairs preserved.
- Final road geometry reconciled to the current native v3 reference:3.6m lanes, centres−4.2/−0.6/+3.0, yellow median lines and362native meshes. The empty background road and all-head-on rule follow user instructions.
- Completed final mainline(v5) and both-bypass(v6) runs, each with21cars/20logs and all3lanes. Actual chapter clear, no observed wrong directions/roadblock overlaps/three-lane vehicle contact walls. Prior failed cohorts remain retained; v6 fixes driver reaction to the visible log-warning period, without modifying the saved game configuration.
-22focused and5existing gimmick tests pass. Runtime/Editor builds pass with the existing SplineSpeed warning. Agent-harness validation passes.
- Verified Play restart without source recompilation: static traffic suppression resets and Combat Harness count is1during play/0after exit. Spill disable resets its phase before re-enable.
- All77preference keys and original start-scene selector restored. HighWay clean in Edit Mode; RestStop/Data.xlsx unchanged by hash.
- Native screenshot gallery built and browser-verified at `http://127.0.0.1:8776/three-lanes/applied/`. README and ce-compound learning updated. Work complete; no commit/push requested.

## Limits

The9999preset is applied once at start, with normal lateral movement, real collisions and no health pin/teleport. These checks verify routes, direction, obstacle spacing and lifecycle, not campaign balance or Android performance. The existing camera/HUD remain; the opposite outer lane can be cropped near the player when moving to an edge.
