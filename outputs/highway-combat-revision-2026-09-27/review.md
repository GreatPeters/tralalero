# Sequential implementation review

Scope is this request's before/ snapshots, modified shared runtime, new pickup/speed-line components, data migration, native presentation tool and QA tools. Unrelated dirty files are preserved. No subagents or commits.

## Corrections made during execution

- The prior always-open-lane admission policy conflicted with the latest request. Whole rows now spawn together; their movement is coupled until a member is destroyed. The saved-row contract verifies coverage and an opened passage after a weak member is removed.
- Stage-distance following alone did not prevent long cars intersecting on bends. Actual sampled world poses and Physics.ComputePenetration now constrain spawning and group movement. The subsequent cohorts record zero overlap frames.
- A driver that selected only living cars moved into a surviving car even when continued fire had already cleared the next row's lane. QA now reconstructs dead slots using a surviving group member's movement. This was a driver error, separate from balance.
- Mandatory moving combat has less firing time than parked accidents. After retained failed cohorts, workbook exposure budgets are1.6ordinary/1.1rush/1.2swarm seconds; parked accidents retain3.8. The type multipliers and growth curve remain explicit formulas. Formula caches now use the same final authored station as their formula reference.
- The apparent pre-contact damage text came from a predictive warning component. Vehicle actors skip its bootstrap and the warning also refuses to render if attached later. Actual damage still follows collision callbacks; accepted contact logs include the overlapping vehicle IDs.
- The initial shader assertion used an undeclared TMP material property. Source inspection shows a CanvasRenderer-owned draw-time uniform; inspecting a serialized material float was invalid. The corrected contract checks world-space/non-overlay setup, and directed probes check actual visibility behavior.
- Projected police livery initially reached an open window cutout. The projection failure identified the exact point; bands were moved below the window opening. No generated mesh or API key was needed.
- Final image review caught the wide toll canopy's left supports extending into the opposite carriageway. They were moved to the median centre, and a fresh native layout capture verifies the saved cosmetic correction. The actual pickup volumes were unchanged.
- A dependent shell sequence advanced to a lobby after Prepare failed. It was stopped, no game start was triggered, and the task's original registry snapshot was restored byte-identically. Subsequent dependent CLI operations inspect both the outer and inner success flags.

## Review checks

- Other scenes are outside the native installers. Vehicle-specific hooks preserve ordinary enemy warning behavior and normal player speed in other scenes.
- Exit collection requires a live player, a running game, an actual volume overlap and the touched side. One selection consumes both cards; missing them grants nothing. There is no toll slowdown/popup or distance-only reward grant.
- Rush speed is derived from running state, selected branch and the half-open stage interval; merge and disable restore the baseline. Screen streaks have no raycast target and avoid the HUD/road centre.
- Log damage uses the oriented rendered body's collider and remains single-hit per event.
- Cosmetic parts have no unintended contact colliders; pickup volumes and the log are deliberate. Sparkle scripts are disabled and particles are scene-owned.
- Workbook mutation preserves unrelated ZIP parts and other-chapter rows. Replaced IDs are removed rather than leaving stale placement references.
- Normal QA never disables collision, pins HP, teleports or enables9999. Directed fixtures and no-fire tests are labeled separately. User preferences and start scene must be restored after each cohort.

Final accepted runs are play-v4first entry/jam-hi-pass and play-v5open-cash. Both full routes clear; all measured overlap/visibility/warning/damage-consistency counters are zero. no-fire-v1fails at72.8seconds with0shots, and all16directed assertions pass. Original registry bytes/start scene are restored and the scene is clean.82focused test cases pass; the final15workbook methods were directly invoked after a stalled async run was cancelled, with that mode recorded explicitly. Both builds/harness pass. No unresolved implementation finding remains from this review. Physical phone performance, earned-purchase campaign timing and human win rate are not certified.
